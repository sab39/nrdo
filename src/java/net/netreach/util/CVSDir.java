///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.util.misc.CVSDir
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2001/04/02
// Description: Manipulate files held in CVS.
///////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////////////
// nrdo - Object-Relational database development tool (runtime libraries).
// Copyright (c) 2000-2001 NetReach, Inc.
//
//  This library is free software; you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation; either
//  version 2 of the License, or (at your option) any later version.
//
//  This library is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//  Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307  USA
//
// The GNU Lesser General Public License should be located in the file
// COPYING.lgpl. For more information on the specific license terms for nrdo,
// please see the file COPYING.
//
// For more information about nrdo, please contact nrdo@netreach.net or
// write to Stuart Ballard at NetReach Inc, 1 eCommerce Plaza, 124 S Maple
// Street, Ambler, PA  19002  USA.
///////////////////////////////////////////////////////////////////////////////

package net.netreach.util;

// Collections classes used to implement the appropriate behavior.
import java.io.BufferedReader;
import java.io.File;
import java.io.FileReader;
import java.io.FileWriter;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.Reader;
import java.io.Writer;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collections;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Iterator;
import java.util.List;

/**
 * Manipulate files stored in CVS.
 * 
 * @author Stuart Ballard &lt;sballard@netreach.net&gt;
 */
public class CVSDir {
  // TODO:
  // - When throwing an exception due to cvs failure, print or include the full
  // contents of cvs's stdout and stderr.
  class Entry {
    String name;
    int state;
    String version;

    /* private */Entry(String name, int state, String version) {
      this.name = name;
      this.state = state;
      this.version = version;
      entries.put(name, this);
    }

    /* private */Entry(String name, int state) {
      // this(name, state, null);
      this.name = name;
      this.state = state;
      this.version = null;
      entries.put(name, this);
    }

    /* private */Entry(String name) {
      // this(name, UNMODIFIED, null);
      this.name = name;
      this.state = UNMODIFIED;
      this.version = null;
      entries.put(name, this);
    }

    private void syncState() {
      if (state == HALF_ADDED)
        state = LOCALLY_ADDED;
      else if (state == HALF_REMOVED) state = LOCALLY_REMOVED;
    }

    private void fixState() {
      if (state == EXTERNALLY_ADDED)
        state = HALF_ADDED;
      else if (state == EXTERNALLY_REMOVED) state = HALF_REMOVED;
    }

    private void unfixState() {
      if (state == HALF_ADDED)
        state = EXTERNALLY_ADDED;
      else if (state == HALF_REMOVED) state = EXTERNALLY_REMOVED;
    }

    private void commitState() {
      if (state == LOCALLY_ADDED || state == MODIFIED)
        state = UNMODIFIED;
      else if (state == LOCALLY_REMOVED) state = NON_EXISTENT;
    }

    void sync() throws IOException, InterruptedException {
      CVSDir.this.sync(this);
    }

    void fix() {
      CVSDir.this.fix(this);
    }

    void unfix() {
      CVSDir.this.unfix(this);
    }

    void commit() throws IOException, InterruptedException {
      CVSDir.this.commit(this);
    }
  }

  private static final HashMap dirs = new HashMap();

  final File dir;
  private final boolean inCVS;
  final boolean inVSS;
  private final HashMap entries = new HashMap();

  private CVSDir(File dir) throws IOException, InterruptedException {
    this.dir = dir;
    inCVS = cvsEnabled && new File(dir, "CVS").isDirectory();
    inVSS = vssEnabled && new File(dir, "vssver.scc").isFile();
    if (inCVS && inVSS)
      throw new UnsupportedOperationException(
          "Can't handle files that are in both cvs and vss");
    rescan();
  }

  public static CVSDir getInstance(File dir) throws IOException,
      InterruptedException {
    dir = PathUtil.canonicalFile(dir);
    CVSDir instance;
    synchronized (dirs) {
      if (dirs.containsKey(dir)) {
        instance = (CVSDir) dirs.get(dir);
      } else {
        instance = new CVSDir(dir);
        dirs.put(dir, instance);
      }
    }
    return instance;
  }

  Entry getEntry(String name) {
    Entry ent = (Entry) entries.get(name);
    if (ent == null) ent = new Entry(name, NON_EXISTENT);
    return ent;
  }

  private static String vssRoot;

  public void setVssRoot(String root) {
    // These methods are called as if the variables they set aren't static
    // Probably could use some fixing.
    vssRoot = root;
  }

  private static String vssDir;

  public void setVssDir(String dir) {
    // These methods are called as if the variables they set aren't static
    // Probably could use some fixing.
    vssDir = dir;
  }

  private boolean vssAggressive = false;

  public void setVssAggressive(boolean aggressive) {
    vssAggressive = aggressive;
  }

  private static boolean cvsEnabled = false;

  public static void setCVSEnabled(boolean cvsEnabled) {
    CVSDir.cvsEnabled = cvsEnabled;
  }

  private static boolean vssEnabled = false;

  public static void setVSSEnabled(boolean vssEnabled) {
    CVSDir.vssEnabled = vssEnabled;
  }

  public static void reset() {
    dirs.clear();
    vssRoot = null;
    vssDir = null;
    cvsEnabled = false;
    vssEnabled = false;
    vssUsername = null;
    vssPath = null;
  }

  /**
   * A file that doesn't exist at all. This is supported so that CVSFile can
   * still store an Entry for non-existent files (and then call create() on it).
   */
  public static final int NON_EXISTENT = 0;

  /**
   * Not in CVS (either cvsignored or in a directory that doesn't contain a CVS
   * directory).
   */
  public static final int NOT_VERSIONED = 1;

  /**
   * No local modifications (no message or U or P).
   */
  public static final int UNMODIFIED = 2;

  /**
   * Local modifications (M).
   */
  public static final int MODIFIED = 3;

  /**
   * Locally added and CVS added (A).
   */
  public static final int LOCALLY_ADDED = 4;

  /**
   * Locally added through this API but not CVS added (?).
   */
  public static final int HALF_ADDED = 5;

  /**
   * Locally added by some other means but not CVS added (?).
   */
  public static final int EXTERNALLY_ADDED = 6;

  /**
   * Locally removed and CVS removed (R).
   */
  public static final int LOCALLY_REMOVED = 7;

  /**
   * Locally removed through this API but not CVS removed (U with warning).
   */
  public static final int HALF_REMOVED = 8;

  /**
   * Locally removed by some other means but not CVS removed (U with warning).
   */
  public static final int EXTERNALLY_REMOVED = 9;

  /**
   * Contains conflicts (C). CVSDir does not know how to handle this state, but
   * it can track it.
   */
  public static final int CONFLICTED = 10;

  /**
   * Out of date (U, P or M with merge warning, but currently not implemented
   * for CVS). Currently implemented only for SourceSafe: any file not currently
   * checked out by the current user is considered out in this state.
   */
  public static final int OUT_OF_DATE = 11;

  /**
   * Created by some other user but not yet in working copy (U on a file that
   * doesn't exist; not currently implemented anywhere, or used).
   */
  public static final int NOT_YET_EXISTENT = 12;

  public static int compareVersions(String v1, String v2) {
    if (v1.length() == 0) {
      return v2.length() == 0 ? 0 : -1;
    } else if (v2.length() == 0) {
      return 1;
    } else {
      int p1 = v1.indexOf('.');
      int p2 = v2.indexOf('.');
      if (p1 < 0) p1 = v1.length();
      if (p2 < 0) p2 = v2.length();
      int root1 = Integer.valueOf(v1.substring(0, p1)).intValue();
      int root2 = Integer.valueOf(v2.substring(0, p2)).intValue();
      if (root1 < root2) {
        return -1;
      } else if (root1 > root2) {
        return 1;
      } else {
        if (p1 < v1.length()) p1++;
        if (p2 < v2.length()) p2++;
        return compareVersions(v1.substring(p1), v2.substring(p2));
      }
    }
  }

  public void rescan() throws IOException, InterruptedException {
    rescan(false);
  }

  static String vssUsername = null;
  static String vssPath = null;

  void vssUpdateCmd(String uname) throws IOException, InterruptedException {
    vssUpdateCmd(uname, false, null);
  }

  private void vssUpdateCmd(String uname, boolean update, HashSet touched)
      throws IOException, InterruptedException {
    if (uname == null) {
      for (Iterator i = entries.values().iterator(); i.hasNext();) {
        Entry ent = (Entry) i.next();
        vssUpdateCmd(ent.name, update, touched);
      }
      return;
    }
    Entry ent = getEntry(uname);
    if (touched != null) touched.add(uname);

    if (!inVSS || vssRoot == null) {
      ent.state = NOT_VERSIONED;
      return;
    }

    if (update)
      throw new UnsupportedOperationException(
          "updateCmd with update flag set only supported for cvs");

    // Plan: We only care about the following states:
    // MODIFIED: any file that is checked out by the current user.
    // OUT_OF_DATE: any other file that's known to sourcesafe.
    // LOCALLY_ADDED: anything that isn't known to sourcesafe.

    // In aggressive mode, this is changed as follows:
    // MODIFIED: any file that's checked out by the current user
    // UNMODIFIED: any file that isn't checked out at all (this isn't sufficient
    // by itself to produce aggressive behavior; there is code
    // in TableCreator which treats UNMODIFIED as if it were MODIFIED for some
    // purposes)
    // OUT_OF_DATE: any file that's checked out by a different user.

    // Implementation: "ss status SSPATH".
    // This gives the following responses:
    // "No checked out files found" => OUT_OF_DATE (by our definition) or
    // UNMODIFIED in aggressive mode
    // "SSPATH is not an existing filename or project" => LOCALLY_ADDED
    // "fname username Exc date time" => MODIFIED if username is right,
    // otherwise
    // OUT_OF_DATE (by our definition).
    // fname may be truncated; username may be miscapitalized; multiple spaces
    // may appear.

    if (vssUsername == null) {
      vssUsername = System.getProperty("user.name");
      // We need to find ss.exe - try some likely locations.
      if (new File(
          "C:\\Program Files\\Microsoft Visual Studio .NET\\win32\\ss.exe")
          .exists()) {
        vssPath = "C:\\Program Files\\Microsoft Visual Studio .NET\\win32\\ss.exe";
      } else if (new File(
          "C:\\Program Files\\Microsoft Visual Studio\\Common\\Vss\\win32\\ss.exe")
          .exists()) {
        vssPath = "C:\\Program Files\\Microsoft Visual Studio\\Common\\Vss\\win32\\ss.exe";
      } else if (new File(
          "C:\\Program Files\\Microsoft Visual Studio\\Vss\\win32\\ss.exe")
          .exists()) {
        vssPath = "C:\\Program Files\\Microsoft Visual Studio\\Vss\\win32\\ss.exe";
      } else if (new File("C:\\Program Files\\Vss\\win32\\ss.exe").exists()) {
        vssPath = "C:\\Program Files\\Vss\\win32\\ss.exe";
      } else if (new File(
          "C:\\Program Files\\Microsoft Visual Studio .NET\\VSS\\win32\\ss.exe")
          .exists()) {
        vssPath = "C:\\Program Files\\Microsoft Visual Studio .NET\\VSS\\win32\\ss.exe";
      } else if (new File(
          "C:\\program files\\microsoft visual studio .net 2003\\win32\\ss.exe")
          .exists()) {
        vssPath = "C:\\program files\\microsoft visual studio .net 2003\\win32\\ss.exe";
      } else if (new File("C:\\Program Files\\Visual Sourcesafe\\win32\\ss.exe")
          .exists()) {
        vssPath = "C:\\Program Files\\Visual Sourcesafe\\win32\\ss.exe";
      } else {
        throw new IOException("Can't find ss.exe on your machine");
      }
    }

    String[] cmd = new String[] { vssPath, "status", vssRoot + "/" + uname };
    String[] env = new String[] { "ssuser=" + vssUsername, "ssdir=" + vssDir };
    Process proc = Runtime.getRuntime().exec(cmd, env, dir);

    // We're only interested in the first line of output.
    BufferedReader ssOut = new BufferedReader(new InputStreamReader(proc
        .getInputStream()));
    String line = ssOut.readLine();
    if (line == null) {
      BufferedReader ssErr = new BufferedReader(new InputStreamReader(proc
          .getErrorStream()));
      line = ssErr.readLine();
    }
    if (line.startsWith("No checked out files found.")) {
      ent.state = vssAggressive ? UNMODIFIED : OUT_OF_DATE;
    } else if (line.indexOf("is not an existing filename or project") >= 0) {
      ent.state = LOCALLY_ADDED;
    } else {
      int pos = line.indexOf(' ');
      while (line.charAt(++pos) == ' ')
        ;
      int end = line.indexOf(' ', pos);
      String checkoutUser = line.substring(pos, end);
      boolean self = checkoutUser.equalsIgnoreCase(vssUsername);
      System.out.println(vssRoot + "/" + uname + " checked out by "
          + checkoutUser + (self ? " (You)" : " (Not you)"));
      ent.state = self ? MODIFIED : OUT_OF_DATE;
    }
    ssOut.close();
    proc.waitFor();

    // int rv = proc.waitFor();
    // if (rv != 0) throw new IOException(Arrays.asList(cmd).toString() +
    // " failed with error code " + rv);
  }

  private void updateCmd(String uname, boolean update, HashSet touched)
      throws IOException, InterruptedException {

    if (!inCVS) return;

    // Run cvs -nq update -l, or cvs -q update -l if we are actually doing an
    // update rather than a rescan. Replace "-l" with name if there is one.
    String[] cmd = new String[] { "cvs", update ? "-q" : "-nq", "update",
        uname != null ? uname : "-l" };
    Process proc = Runtime.getRuntime().exec(cmd, null, dir);

    // Iterate over the lines in the stdout of cvs.
    BufferedReader cvsOut = new BufferedReader(new InputStreamReader(proc
        .getInputStream()));
    String line = "";
    while (line != null) {
      line = cvsOut.readLine();
      if (line == null) continue;

      // We are only interested in lines with a single-char status code at the
      // beginning, followed by a space.
      if (line.charAt(1) == ' ') {
        String name = line.substring(2);
        if (touched != null) touched.add(name);

        // Store the state of the current entry for this name. It will affect
        // what we do with EXTERNALLY_* entries.
        Entry ent = getEntry(name);

        // Depending on the first character, we create an entry with the
        // appropriate status.
        switch (line.charAt(0)) {
        case 'U':
          if (update || new File(dir, name).isFile()) {
            ent.state = UNMODIFIED;
          } else {
            if (ent.state != HALF_REMOVED) ent.state = NON_EXISTENT;
          }
          break;
        case '?':
          if (ent.state != HALF_ADDED) ent.state = EXTERNALLY_ADDED;
          break;
        case 'R':
          ent.state = LOCALLY_REMOVED;
          break;
        case 'A':
          ent.state = LOCALLY_ADDED;
          break;
        case 'P':
          ent.state = UNMODIFIED;
          break;
        case 'M':
          ent.state = MODIFIED;
          break;
        case 'C':
          ent.state = CONFLICTED;
          break;
        }
      }
    }
    cvsOut.close();
    int rv = proc.waitFor();

    if (rv != 0)
      throw new IOException(Arrays.asList(cmd).toString()
          + " failed with error code " + rv);

    rescanVersions(uname, touched);
  }

  private void rescanVersions(String uname, HashSet touched) throws IOException {

    if (inVSS)
      throw new UnsupportedOperationException(
          "rescanVersions implemented for cvs only");
    if (!inCVS) return;

    // Scan for version numbers.
    BufferedReader entFile = new BufferedReader(new FileReader(new File(dir,
        "CVS" + File.separator + "Entries")));
    String line = "";
    while (line != null) {
      line = entFile.readLine();
      if (line == null) continue;

      if (line.charAt(0) == '/'
          && (uname == null || line.startsWith("/" + uname + "/"))) {
        String name = line.substring(1, line.indexOf('/', 1));
        if (touched != null) touched.add(name);

        // Update the entry with the version number.
        Entry ent = getEntry(name);
        ent.version = line.substring(name.length() + 2, line.indexOf('/', name
            .length() + 2));
        if (ent.state == NON_EXISTENT) ent.state = EXTERNALLY_REMOVED;
      }
    }
    entFile.close();
  }

  private synchronized void rescan(boolean update) throws IOException,
      InterruptedException {

    HashSet cvsignore = new HashSet();
    if (inCVS) {
      // Read .cvsignore if present
    }

    HashSet touched = new HashSet();

    File[] files = dir.listFiles();
    for (int i = 0; i < files.length; i++) {
      File f = files[i];
      if (f.isFile()) {
        Entry ent = getEntry(f.getName());
        if (ent.state == NON_EXISTENT || ent.state == LOCALLY_REMOVED
            || ent.state == HALF_REMOVED) {
          if (!inCVS || cvsignore.contains(f.getName())) {
            ent.state = NOT_VERSIONED;
          } else {
            ent.state = UNMODIFIED;
          }
        }
        touched.add(f.getName());
      }
    }

    // Run cvs update across the whole directory and interpret the results.
    updateCmd(null, update, touched);

    // Anything that hasn't been touched no longer exists at all, so we can
    // throw it away.
    for (Iterator i = entries.values().iterator(); i.hasNext();) {
      Entry ent = (Entry) i.next();
      if (!touched.contains(ent.name)) {
        ent.state = NON_EXISTENT;
      }
    }
  }

  private class StreamPrinter extends Thread {
    InputStream is;

    /* private */StreamPrinter(InputStream is) {
      this.is = is;
    }

    public void run() {
      try {
        int i;
        while ((i = is.read()) >= 0) {
          if (verboseCVS) System.out.write((char) i);
        }
      } catch (IOException e) {
        e.printStackTrace();
      } finally {
        try {
          is.close();
        } catch (IOException e) {
        }
      }
    }
  }

  public boolean verboseCVS = false;

  public void cvs(String cmd, List args) throws IOException,
      InterruptedException {
    if (inVSS)
      throw new UnsupportedOperationException(
          "cvs can't be run on a vss repository!");
    if (!inCVS) return;
    String[] command = new String[args.size() + 2];
    command[0] = "cvs";
    command[1] = cmd;
    int ct = 2;
    for (Iterator i = args.iterator(); i.hasNext();) {
      command[ct++] = (String) i.next();
    }
    debugMsg("About to run " + Arrays.asList(command) + " in " + dir);
    StreamPrinter spOut = new StreamPrinter(null);
    StreamPrinter spErr = new StreamPrinter(null);
    Process proc = Runtime.getRuntime().exec(command, null, dir);
    debugMsg("Process started, waiting for it to terminate");
    spOut.is = proc.getInputStream();
    spErr.is = proc.getErrorStream();
    spOut.start();
    spErr.start();
    int rv = proc.waitFor();
    spOut.join();
    spErr.join();
    debugMsg("Process finished, rv=" + rv);
    if (rv != 0)
      throw new IOException("cvs " + cmd + args + " in " + dir
          + " failed with error code " + rv);
  }

  public void update() throws IOException, InterruptedException {
    rescan(true);
  }

  public void update(String name) throws IOException, InterruptedException {
    updateCmd(name, true, null);
  }

  public void update(Entry ent) throws IOException, InterruptedException {
    update(ent.name);
  }

  public synchronized void fix() {
    for (Iterator i = entries.values().iterator(); i.hasNext();) {
      Entry ent = (Entry) i.next();
      ent.fixState();
    }
  }

  public synchronized void fix(String name) {
    fix(getEntry(name));
  }

  public synchronized void fix(Entry ent) {
    if (inVSS)
      throw new UnsupportedOperationException("fix() is only supported for cvs");
    ent.fixState();
  }

  public synchronized void unfix() {
    for (Iterator i = entries.values().iterator(); i.hasNext();) {
      Entry ent = (Entry) i.next();
      ent.unfixState();
    }
  }

  public synchronized void unfix(String name) {
    unfix(getEntry(name));
  }

  public synchronized void unfix(Entry ent) {
    if (inVSS)
      throw new UnsupportedOperationException(
          "unfix() is only supported for cvs");
    ent.unfixState();
  }

  public synchronized void sync() throws IOException, InterruptedException {
    if (inVSS)
      throw new UnsupportedOperationException(
          "sync() is only supported for cvs");
    if (!inCVS) return;
    List adds = new ArrayList();
    List removes = new ArrayList();
    for (Iterator i = entries.values().iterator(); i.hasNext();) {
      Entry ent = (Entry) i.next();
      if (ent.state == HALF_ADDED)
        adds.add(ent.name);
      else if (ent.state == HALF_REMOVED) removes.add(ent.name);
    }
    if (!removes.isEmpty()) cvs("remove", removes);
    if (!adds.isEmpty()) cvs("add", adds);
    for (Iterator i = entries.values().iterator(); i.hasNext();) {
      ((Entry) i.next()).syncState();
    }
  }

  public synchronized void sync(String name) throws IOException,
      InterruptedException {
    sync(getEntry(name));
  }

  public synchronized void sync(Entry ent) throws IOException,
      InterruptedException {
    if (inVSS)
      throw new UnsupportedOperationException(
          "sync(Entry) is only supported for cvs");
    if (!inCVS) return;
    List l = Collections.singletonList(ent.name);
    if (ent.state == HALF_ADDED)
      cvs("add", l);
    else if (ent.state == HALF_REMOVED) cvs("remove", l);
    ent.syncState();
  }

  void debugMsg(String msg) {
    if (verboseCVS) {
      System.err.println(msg);
      System.err.flush();
    }
  }

  public void commit() throws IOException, InterruptedException {
    commit("\"\"");
  }

  public synchronized void commit(String msg) throws IOException,
      InterruptedException {
    if (inVSS)
      throw new UnsupportedOperationException(
          "commit is only supported for cvs");
    if (!inCVS) return;
    debugMsg("Committing " + dir);
    debugMsg("About to sync states");
    sync();
    debugMsg("States sunc, about to construct command");
    List command = new ArrayList();
    command.add("-l"); // Local mode (do not recurse)
    command.add("-m");
    command.add(msg); // Commit message
    debugMsg("command constructed, about to cvs commit " + command);
    cvs("commit", command);
    debugMsg("cvs commit " + command + " complete, updating states");
    for (Iterator i = entries.values().iterator(); i.hasNext();) {
      Entry ent = (Entry) i.next();
      ent.commitState();
    }
    debugMsg("states updated, about to rescan versions");
    rescanVersions(null, null);
    debugMsg("versions rescanned");
  }

  public synchronized void commit(String name, String msg) throws IOException,
      InterruptedException {
    commit(getEntry(name), msg);
  }

  public void commit(Entry ent) throws IOException, InterruptedException {
    commit(ent, "\"\"");
  }

  public synchronized void commit(Entry ent, String msg) throws IOException,
      InterruptedException {
    if (inVSS)
      throw new UnsupportedOperationException(
          "commit(Entry) is only supported for cvs");
    if (!inCVS) return;
    sync(ent);
    List command = new ArrayList();
    command.add("-l"); // Local mode (do not recurse)
    command.add("-m");
    command.add(msg); // Commit message
    command.add(ent.name);
    cvs("commit", command);
    ent.commitState();
    rescanVersions(ent.name, null);
  }

  public synchronized Reader getReader(String name) throws IOException {
    return getReader(getEntry(name));
  }

  public synchronized Reader getReader(Entry ent) throws IOException {
    return new FileReader(new File(dir, ent.name));
  }

  public synchronized Writer getWriter(String name) throws IOException {
    return getWriter(getEntry(name));
  }

  public synchronized Writer getWriter(Entry ent) throws IOException {
    if (inVSS)
      throw new UnsupportedOperationException(
          "getWriter is only supported for cvs");
    if (inCVS && !create(ent)) {
      if (ent.state == EXTERNALLY_ADDED) {
        ent.state = HALF_ADDED;
      } else if (ent.state != HALF_ADDED) {
        ent.state = MODIFIED;
      }
    }
    return new FileWriter(new File(dir, ent.name));
  }

  public synchronized boolean create(String name) throws IOException {
    return create(getEntry(name));
  }

  public synchronized boolean create(Entry ent) throws IOException {
    if (inVSS)
      throw new UnsupportedOperationException(
          "create is only supported for cvs");
    boolean rv = new File(dir, ent.name).createNewFile();
    if (rv) {
      if (!inCVS) {
        ent.state = NOT_VERSIONED;
      } else if (ent.state == HALF_REMOVED || ent.state == EXTERNALLY_REMOVED) {
        ent.state = MODIFIED;
      } else {
        ent.state = HALF_ADDED;
      }
    }
    return rv;
  }

  public synchronized void delete(String name) throws IOException {
    delete(getEntry(name));
  }

  public synchronized void delete(Entry ent) throws IOException {
    if (inVSS)
      throw new UnsupportedOperationException(
          "delete is only supported for cvs");
    new File(dir, ent.name).delete();
    if (!inCVS || ent.state == HALF_ADDED || ent.state == EXTERNALLY_ADDED) {
      ent.state = NON_EXISTENT;
    } else {
      ent.state = HALF_REMOVED;
    }
  }

  public String toString() {
    return dir.toString();
  }
}
