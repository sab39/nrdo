///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.util.misc.CVSFile
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2001/03/07
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
import java.io.File;
import java.io.IOException;
import java.io.Reader;
import java.io.Writer;

/**
 * Manipulate files stored in CVS.
 * 
 * @author Stuart Ballard &lt;sballard@netreach.net&gt;
 */
public class CVSFile {

  private CVSDir parent;
  private CVSDir.Entry ent;

  public CVSFile(CVSDir parent, String name) throws IOException,
      InterruptedException {
    this.parent = parent;
    ent = parent.getEntry(name);
  }

  public CVSFile(File file) throws IOException, InterruptedException {
    this(CVSDir.getInstance(file.getParentFile()), file.getName());
  }

  public CVSFile(File file, String name) throws IOException,
      InterruptedException {
    this(CVSDir.getInstance(file), name);
  }

  public CVSFile(String name) throws IOException, InterruptedException {
    this(new File(name));
  }

  public CVSDir getParent() {
    return parent;
  }

  public long lastModified() {
    return new File(parent.dir, ent.name).lastModified();
  }

  public boolean exists() {
    return ent.state != CVSDir.NON_EXISTENT
        && ent.state != CVSDir.LOCALLY_REMOVED
        && ent.state != CVSDir.HALF_REMOVED
        && ent.state != CVSDir.EXTERNALLY_REMOVED;
  }

  public boolean isKnown() {
    return ent.state != CVSDir.NON_EXISTENT;
  }

  public int getState() throws IOException, InterruptedException {
    if (parent.inVSS) parent.vssUpdateCmd(ent.name);
    return ent.state;
  }

  public boolean create() throws IOException {
    return parent.create(ent);
  }

  public void delete() throws IOException {
    parent.delete(ent);
  }

  public void renameTo(String newName) throws IOException {
    CVSDir.Entry newEnt = parent.getEntry(newName);
    if (!parent.create(newEnt))
      throw new IOException("File " + newName + " already exists");

    String contents = getContents();
    CVSDir.Entry oldEnt = ent;
    ent = newEnt;
    setContents(contents);
    parent.delete(oldEnt);
  }

  public String getVersion() {
    if (ent.state == CVSDir.MODIFIED || ent.state == CVSDir.UNMODIFIED) {
      return ent.version;
    } else {
      return "";
    }
  }

  public String getContents() throws IOException {
    Reader r = getReader();
    StringBuffer sb = new StringBuffer();
    int ch;
    while ((ch = r.read()) >= 0)
      sb.append((char) ch);
    r.close();
    return sb.toString();
  }

  public void setContents(String contents) throws IOException {
    Writer w = getWriter();
    w.write(contents);
    w.close();
  }

  public Reader getReader() throws IOException {
    return parent.getReader(ent);
  }

  public Writer getWriter() throws IOException {
    return parent.getWriter(ent);
  }

  public void commit() throws InterruptedException, IOException {
    parent.commit(ent);
  }

  public void commit(String msg) throws InterruptedException, IOException {
    parent.commit(ent, msg);
  }

  public void commitAll() throws InterruptedException, IOException {
    parent.commit();
  }

  public void commitAll(String msg) throws InterruptedException, IOException {
    parent.commit(msg);
  }

  public void update() throws InterruptedException, IOException {
    parent.update(ent);
  }

  public void updateAll() throws InterruptedException, IOException {
    parent.update();
  }

  public void fix() {
    parent.fix(ent);
  }

  public void fixAll() {
    parent.fix();
  }

  public void unfix() {
    parent.unfix(ent);
  }

  public void unfixAll() {
    parent.unfix();
  }

  public void sync() throws InterruptedException, IOException {
    parent.sync(ent);
  }

  public void syncAll() throws InterruptedException, IOException {
    parent.sync();
  }

  public String toString() {
    return parent.toString() + File.separator + ent.name;
  }
}
