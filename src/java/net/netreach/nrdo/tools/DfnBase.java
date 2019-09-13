///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.nrdo.tools.DfnBase
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2000/10/16
// Description: A pool of TableDef objects instantiated from a given path.
///////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////////////
// nrdo - Object-Relational database development tool.
// Copyright (c) 2000-2001 NetReach, Inc.
//
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// The GNU General Public License should be located in the file COPYING.gpl.
// For more information on the specific license terms for nrdo, please see the
// file COPYING.
//
// For more information about nrdo, please contact nrdo@netreach.net or
// write to Stuart Ballard at NetReach Inc, 1 eCommerce Plaza, 124 S Maple
// Street, Ambler, PA  19002  USA.
///////////////////////////////////////////////////////////////////////////////

package net.netreach.nrdo.tools;

// Collections classes used to implement the appropriate behavior.
import java.io.BufferedReader;
import java.io.File;
import java.io.FileReader;
import java.io.IOException;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Iterator;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.TreeMap;

import net.netreach.util.FileLocatedException;
import net.netreach.util.Output;

/**
 * A pool of TableDef objects instantiated from a given path.
 * 
 * @author Stuart Ballard &lt;sballard@netreach.net&gt;
 */
public class DfnBase {

  private Map defs = new TreeMap();
  private Map queries = new TreeMap();
  List dfnPath = new ArrayList();
  Set exclude = new HashSet();
  boolean verbose = false;
  public Config cfg;
  Set touchedFiles = new HashSet();

  public DfnBase(String dfnPath) throws IOException {
    this();
    this.dfnPath.add(dfnPath);
  }

  public DfnBase() {
  }

  public void addPath(String pth) {
    // We only want to allow the path to be modified at startup
    if (!defs.isEmpty()) throw new RuntimeException("Can't modify path");
    dfnPath.add(pth);
  }

  // Note: Modifier support is currently very incomplete. The final behavior
  // will allow +foo and -foo, and operate on a per-dfnpath basis. The current
  // behavior is global and only allows -foo.
  public void addModifier(String modifier) {
    if (modifier.startsWith("-")) {
      exclude.add(modifier.substring(1));
    }
  }

  public TableDef load(String tableName) throws FileLocatedException, IOException {
    TableDef td = loadMinimum(tableName);
    if (td != null) td.resolveRefs();
    return td;
  }

  TableDef loadGetsFully(String tableName) throws FileLocatedException, IOException {
    TableDef td = loadMinimum(tableName);
    if (td != null) td.resolveGetsFully();
    return td;
  }

  TableDef loadGetsOnly(String tableName) throws FileLocatedException, IOException {
    TableDef td = loadMinimum(tableName);
    if (td != null) td.resolveGets();
    return td;
  }

  TableDef loadFieldsOnly(String tableName) throws FileLocatedException, IOException {
    TableDef td = loadMinimum(tableName);
    if (td != null) td.resolveFields();
    return td;
  }

  TableDef loadMinimum(String tableName) throws IOException {
    TableDef td = (TableDef) defs.get(tableName);
    if (td != null || defs.containsKey(tableName)) return td;

    File file = findFile(tableName, "dfn", "table");
    if (file != null) {
      td = new TableDef(this, file, tableName);
      defs.put(tableName, td);
    }
    if (td == null) {
      for (Iterator i = cfg.depends.iterator(); i.hasNext();) {
        Config depConfig = (Config) i.next();
        td = depConfig.dfnbase.loadMinimum(tableName);
        if (td != null) break;
      }
    }
    return td;
  }

  QueryDef loadQuery(String queryName) throws FileLocatedException, IOException {
    QueryDef qd = loadQueryMinimum(queryName);
    if (qd != null) qd.resolve();
    return qd;
  }

  QueryDef loadQueryMinimum(String queryName) throws IOException {
    QueryDef qd = (QueryDef) queries.get(queryName);
    if (qd != null || queries.containsKey(queryName)) return qd;

    File file = findFile(queryName, "qu", "query");
    if (file != null) qd = new QueryDef(this, file, queryName);
    queries.put(queryName, qd);
    return qd;
  }

  private File findFile(String name, String extension, String type) {
    if (cfg.module != null) {
      // If it's not in the current module, we return null to allow it to be
      // looked for in dependent modules.
      if (!name.startsWith(cfg.module + ":")) return null;
      name = name.substring(cfg.module.length() + 1);
    }
    File file = null;
    boolean skip = false;
    int pos = name.length();
    while (pos > 0) {
      if (exclude.contains(name.substring(0, pos))) skip = true;
      pos = name.lastIndexOf(':', pos - 2) + 1;
    }
    if (!skip) {
      for (Iterator i = dfnPath.listIterator(); file == null && i.hasNext();) {
        String pth = (String) i.next();
        File dir = new File(pth);
        if (dir.isFile()) pth = dir.getParent();
        String fname = name.replace(':', '/') + "." + extension;
        file = new File(pth, fname);
        if (!file.exists()) {
          file = new File(pth, fname.substring(0, 1).toUpperCase()
              + fname.substring(1));
        }
        if (!file.exists()) {
          file = null;
        } else {
          touchedFiles.add(file);
          if (file.length() == 0) file = null;
        }
      }
    }
    if (file == null) {
      Output.reportError(null, "Cannot load " + type + " " + name);
    }
    return file;
  }

  public List filter(List list) {
    for (Iterator i = list.iterator(); i.hasNext();) {
      if (i.next() == null) i.remove();
    }
    return list;
  }

  public List getAll() {
    return filter(new ArrayList(defs.values()));
  }

  public List getAllQueries() {
    return filter(new ArrayList(queries.values()));
  }

  private List dependTables;

  public List getDependTables() throws IOException {
    Map knownTables = new HashMap();
    if (dependTables == null) {
      dependTables = new ArrayList();
      for (Iterator i = cfg.depends.iterator(); i.hasNext();) {
        Config depcfg = (Config) i.next();
        depcfg.dfnbase.search();
        for (Iterator j = depcfg.dfnbase.getAll().iterator(); j.hasNext();) {
          TableDef td = (TableDef) j.next();
          if (!knownTables.containsKey(td.fullName)) {
            knownTables.put(td.fullName, Boolean.TRUE);
            dependTables.add(new DependTable(td));
          }
        }
        for (Iterator j = depcfg.dfnbase.getDependTables().iterator(); j
            .hasNext();) {
          DependTable dt = (DependTable) j.next();
          if (!knownTables.containsKey(dt.getFullName())) {
            knownTables.put(dt.getFullName(), Boolean.TRUE);
            dependTables.add(dt);
          }
        }
      }
    }
    return dependTables;
  }

  public List getFromArgs(String[] args) throws IOException {
    if (args == null || args.length == 0 || (args.length == 1 && args[0].startsWith("@"))) {
      search();
      return getAll();
    } else {
      List l = new ArrayList();
      for (int i = 0; i < args.length; i++) {
        l.add(loadMinimum(args[i]));
      }
      return filter(l);
    }
  }

  public List getQueriesFromArgs(String[] args) throws IOException {
    if (args == null || args.length == 0 || (args.length == 1 && args[0].startsWith("@"))) {
      search();
      return getAllQueries();
    } else {
      List l = new ArrayList();
      for (int i = 0; i < args.length; i++) {
        l.add(loadQueryMinimum(args[i]));
      }
      return filter(l);
    }
  }

  private boolean searched = false;

  private void search() throws IOException {
    if (searched) return;
    for (Iterator i = dfnPath.iterator(); i.hasNext();) {
      search(new File((String) i.next()), cfg.module);
    }
    searched = true;
  }

  private static final String REL_PATH_PREFIX = "RelPath = \"";
  private static final String MSBUILD_PREFIX = "<None Include=\"";
  private static final String MSBUILD_SUFFIX = "\" />";

  private void search(File f, String module) throws IOException {
    String base = module == null ? "" : module + ":";

    // If the path is a directory (the original use case) we just scan it recursively.
    if (f.isDirectory()) {
      File[] files = f.listFiles();
      if (!exclude.contains(base)) {
        for (int i = 0; i < files.length; i++) {
          if (files[i].isDirectory()) {
            search(files[i], base + files[i].getName().toLowerCase());
          } else if (files[i].isFile()) {
            String name = files[i].getName().toLowerCase();
            int pos = name.lastIndexOf('.');
            if (pos > 0) {
              String obname = base + name.substring(0, pos);
              if (!exclude.contains(obname)) {
                if (name.endsWith(".dfn")) {
                  loadMinimum(obname);
                } else if (name.endsWith(".qu")) {
                  loadQueryMinimum(obname);
                }
              }
            }
          }
        }
      }
    } else {
      // If the path is a file, rather than a directory, parse it as a .csproj or
      // .sln file, whichever applies
      if (f.getName().endsWith(".sln")) {
        scanSlnFile(f, base);
      } else {
        scanCsProjFile(f, base);
      }
    }
  }
  
  private void scanSlnFile(File f, String base) throws IOException {
    Output.println("Scanning sln " + f + "...");
    touchedFiles.add(f);
    FileReader fr = new FileReader(f);
    try {
      BufferedReader br = new BufferedReader(fr);
      try {
        String line;
        while ((line = br.readLine()) != null) {
          line = line.trim();
          if (line.startsWith("Project(\"{")) {
            String proj = line.split(",")[1].split("\"")[1];
            String projBase = "";
            if (proj.indexOf('\\') >= 0) {
              projBase = proj.substring(0, proj.lastIndexOf('\\') + 1).replace('\\', ':');
              if (projBase.substring(1).toLowerCase().equals(projBase.substring(1))) {
                projBase = projBase.toLowerCase();
              }
            }
            scanCsProjFile(new File(f.getParentFile(), proj), base + projBase);
          }
        }
      } finally {
        br.close();
      }
    } finally {
      fr.close();
    }
  }
  private void scanCsProjFile(File f, String base) throws IOException {
    touchedFiles.add(f);
    Output.println("Scanning csproj " + f + "...");
    FileReader fr = new FileReader(f);
    try {
      BufferedReader br = new BufferedReader(fr);
      try {
        String line;
        while ((line = br.readLine()) != null) {
          line = line.trim();
          if (line.startsWith("table ")) {
            String name = line.substring(line.indexOf(' ') + 1);
            loadMinimum(name);
          } else if (line.startsWith("query ")) {
            String name = line.substring(line.indexOf(' ') + 1);
            loadQueryMinimum(name);
          } else if ((line.startsWith(REL_PATH_PREFIX) && line.endsWith("\""))
              || (line.startsWith(MSBUILD_PREFIX) && line
                  .endsWith(MSBUILD_SUFFIX))) {
            String fname = line.substring(line.indexOf("\"") + 1, line
                .lastIndexOf("\""));
            if (fname.endsWith(".dfn") || fname.endsWith(".qu")) {
              boolean isQuery = fname.endsWith(".qu");
              fname = fname.substring(0, fname.lastIndexOf('.'));
              int modpos = fname.lastIndexOf('\\');
              String modname = "";
              if (modpos >= 0) {
                modname = fname.substring(0, modpos + 1).replace('\\', ':')
                    .toLowerCase();
                fname = fname.substring(modpos + 1);
              }
              modname = base + modname;
              String obname = modname + fname;
              if (isQuery)
                loadQueryMinimum(obname);
              else
                loadMinimum(obname);
            }
          }
        }
      } finally {
        br.close();
      }
    } finally {
      fr.close();
    }
  }
}
