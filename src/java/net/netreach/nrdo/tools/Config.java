///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.nrdo.tools.Config
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2000/10/16
// Description: Load configuration information for NRDO and store it.
///////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////////////
// nrdo - Object-Relational database development tool.
// Copyright (c) 2000-2002 NetReach, Inc.
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
import java.util.Arrays;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;

import net.netreach.cgl.CGLParser;
import net.netreach.cgl.Definition;
import net.netreach.smelt.ParseException;
import net.netreach.smelt.SmeltReader;
import net.netreach.util.CVSDir;
import net.netreach.util.DefaultOutput;
import net.netreach.util.FileLocatedException;
import net.netreach.util.FileLocation;
import net.netreach.util.LoggingOutputProvider;
import net.netreach.util.Output;
import net.netreach.util.PathUtil;
import net.netreach.util.PromptCmdProvider;

/**
 * Load configuration information for NRDO and store it.
 * 
 * @author Stuart Ballard &lt;sballard@netreach.net&gt;
 */
public class Config {

  // Add new keywords to follow "strict" here
  private static final HashSet legalStrict = new HashSet(Arrays.asList(new String[] {
      "orderby", "deps" }));

  public final boolean verboseerrors = Boolean.TRUE.booleanValue();
  public final Config baseConfig;
  public final List depends = new ArrayList();
  public final File cfgfile;
  public final File root;
  public final long modified;
  public final DfnBase dfnbase;
  public final File srcbase;
  public final File cachebase;
  public final Definition global;
  public final Definition local;
  public final String module;
  public final String vssroot;
  public final String vssdir;
  public final String schema;
  public final String indextablespace;
  public final String role;
  public final String dbdriver;
  public final String dburl;
  public final String dbuser;
  public final String dbpasswd;
  public final boolean verboseCVS;
  public final boolean forcedrops;
  public final boolean nodatabase;
  public final boolean nocode;
  public final File cgltemplate;
  public final File querytemplate;
  public final String dbadaptername;
  public final DBAdapter dbadapter;
  public final TypeMapping dbtypemap = new TypeMapping();
  public final Map globalVars = new HashMap();
  public final File stampfile;
  public final File logsql;
  public final boolean hacknobefore;
  private final HashSet strict = new HashSet();

  private File constructFile(SmeltReader sr, boolean checkExists,
      boolean expectDir) throws IOException, ParseException {
    String path = sr.checkString().replace('/', File.separatorChar);
    File result = new File(path);
    if (!result.isAbsolute()) result = new File(root, path);
    if (checkExists) {
      if (!(expectDir ? result.isDirectory() : result.isFile())) {
        throw new ParseException(sr, "File " + result + " not found");
      }
    }
    sr.readToken();
    return result;
  }

  private Object notNull(Object o, String name, SmeltReader sr)
      throws ParseException {
    if (o == null) {
      throw new ParseException(sr, "'" + name + "' line not found");
    }
    return o;
  }

  private static Map configs = new HashMap();
  public static void reset() {
    configs.clear();
  }

  public static Config get(String filename) throws FileLocatedException, IOException {
    return get(new File(filename));
  }

  public static Config get(File cfgfile) throws FileLocatedException, IOException {
    File canonical = cfgfile.getCanonicalFile();
    if (!configs.containsKey(canonical)) {
      configs.put(canonical, null); // which we check for below...
      configs.put(canonical, new Config(cfgfile));
    }
    Config cfg = (Config) configs.get(canonical);
    if (cfg == null) {
      throw new RuntimeException("Config file " + canonical
          + " depends on itself");
    }
    return cfg;
  }

  private Config(File cfgfile) throws FileLocatedException, IOException {
    this.cfgfile = cfgfile;
    root = cfgfile.getParentFile();

    // Temporary variables to hold values loaded from file.
    Config baseConfig = null;
    DfnBase dfnbase = null;
    File srcbase = null;
    File cgltemplate = null;
    File querytemplate = null;
    File cachebase = null;
    Definition global = null;
    Definition local = null;
    String module = null;
    String schema = null;
    String vssroot = null;
    String vssdir = null;
    String indextablespace = null;
    String role = null;
    String dbdriver = null;
    String dburl = null;
    String dbuser = null;
    String dbpasswd = null;
    boolean verboseCVS = false;
    boolean forcedrops = false;
    boolean nodatabase = false;
    Boolean nocode = null;
    String dbadapter = null;
    File stampfile = null;
    File logsql = null;
    boolean hacknobefore = false;

    SmeltReader sr = new SmeltReader(cfgfile);
    sr.readToken();
    sr.skipToken("config");
    sr.skipSOB();
    while (!sr.wasEOB()) {
      String fword = sr.skipString();
      if ("defaults".equals(fword)) {
        if (baseConfig == null) {
          File file = constructFile(sr, false, false);
          if (file.exists()) baseConfig = Config.get(file);
        } else {
          sr.skipString();
        }
      } else if ("depends".equals(fword)) {
        depends.add(Config.get(constructFile(sr, true, false)));
      } else if ("global".equals(fword)) {
        global = CGLParser.loadDefinition(cfgfile, sr);
      } else if ("local".equals(fword)) {
        local = CGLParser.loadDefinition(cfgfile, sr);
      } else if ("dfnbase".equals(fword)) {
        if (sr.wasString()) {
          dfnbase = new DfnBase(PathUtil.canonicalPath(constructFile(sr, false,
              true)));
          while (!sr.wasEOL())
            dfnbase.addModifier(sr.skipString());
        } else {
          dfnbase = new DfnBase();
          sr.skipSOB();
          while (!sr.wasEOB()) {
            dfnbase.addPath(PathUtil.canonicalPath(constructFile(sr, false,
                true)));
            while (!sr.wasEOL())
              dfnbase.addModifier(sr.skipString());
            sr.skipEOL();
          }
          sr.skipEOB();
        }
        dfnbase.cfg = this;
      } else if ("srcbase".equals(fword)) {
        srcbase = constructFile(sr, false, true);
      } else if ("cachebase".equals(fword)) {
        cachebase = constructFile(sr, true, true);
      } else if ("pkgbase".equals(fword)) {
        Output.reportError(sr.getLastTokenLocation(), "Deprecated keyword 'pkgbase'");
        sr.skipString();
      } else if ("template".equals(fword)) {
        Output.reportError(sr.getLastTokenLocation(), "Deprecated keyword 'template'");
        sr.skipString();
      } else if ("cgltemplate".equals(fword)) {
        cgltemplate = constructFile(sr, true, false);
      } else if ("querytemplate".equals(fword)) {
        querytemplate = constructFile(sr, true, false);
      } else if ("logfile".equals(fword)) {
        Output.addOutputProvider(new LoggingOutputProvider(constructFile(sr, false, false)), false);
      } else if ("usecgl".equals(fword)) {
        Output.reportError(sr.getLastTokenLocation(), "Warning: Deprecated keyword 'usecgl'");
      } else if ("verbosecvs".equals(fword)) {
        CVSDir.setCVSEnabled(true);
        verboseCVS = true;
      } else if ("cvsenabled".equals(fword)) {
        CVSDir.setCVSEnabled(true);
      } else if ("forcedrops".equals(fword)) {
        forcedrops = true;
      } else if ("nodatabase".equals(fword)) {
        nodatabase = true;
      } else if ("nocode".equals(fword)) {
        nocode = true;
      } else if ("docode".equals(fword)) {
        nocode = false;
      } else if ("promptcmd".equals(fword)) {
        ArrayList promptcmd = new ArrayList();
        while (!sr.wasEOL()) {
          promptcmd.add(sr.skipString());
        }
        if (Output.getPromptProvider() instanceof DefaultOutput) {
          Output.setPromptProvider(new PromptCmdProvider(promptcmd));
        }
      } else if ("vssroot".equals(fword)) {
        CVSDir.setVSSEnabled(true);
        vssroot = sr.skipString();
      } else if ("vssdir".equals(fword)) {
        CVSDir.setVSSEnabled(true);
        vssdir = sr.skipString();
      } else if ("module".equals(fword)) {
        module = sr.skipString();
      } else if ("schema".equals(fword)) {
        schema = sr.skipString();
      } else if ("indextablespace".equals(fword)) {
        indextablespace = sr.skipString();
      } else if ("role".equals(fword)) {
        role = sr.skipString();
      } else if ("dbdriver".equals(fword)) {
        dbdriver = sr.skipString();
        if (!"IKVM.NET".equals(System.getProperty("java.vm.name"))) {
          int pos = dbdriver.indexOf(",");
          if (pos >= 0) dbdriver = dbdriver.substring(0, pos);
        }
      } else if ("dburl".equals(fword)) {
        dburl = sr.skipString();
      } else if ("dbuser".equals(fword)) {
        dbuser = sr.skipString();
      } else if ("dbpasswd".equals(fword)) {
        dbpasswd = sr.skipString();
      } else if ("dbadapter".equals(fword)) {
        dbadapter = sr.skipString();
      } else if ("typemap".equals(fword)) {
        sr.skipSOB();
        while (!sr.wasEOB()) {
          sr.skipToken("dbtype");
          String from = sr.skipString();
          String to = sr.skipString();
          dbtypemap.entries.add(new TypeMapping.Entry(from, to));
          sr.skipEOL();
        }
        sr.skipEOB();
      } else if ("stampfile".equals(fword)) {
        stampfile = constructFile(sr, false, false);
      } else if ("logsql".equals(fword)) {
        logsql = constructFile(sr, false, false);
      } else if ("hacknobefore".equals(fword)) {
        hacknobefore = true;
      } else if ("strict".equals(fword)) {
        String strictKey = sr.skipString();
        if (!legalStrict.contains(strictKey))
          throw new ParseException(sr, "Unknown 'strict' keyword " + strictKey
              + " (legal values are " + legalStrict + ")");
        strict.add(strictKey);
      } else {
        throw new ParseException(sr, "Unknown configuration keyword '" + fword
            + "'");
      }
      sr.skipEOL();
    }
    sr.skipEOB();
    sr.skipEOL();
    sr.checkEOF();

    if (baseConfig != null) {
      if (!verboseCVS) verboseCVS = baseConfig.verboseCVS;
      if (!forcedrops) forcedrops = baseConfig.forcedrops;
      if (!nodatabase) nodatabase = baseConfig.nodatabase;
      if (nocode == null) nocode = baseConfig.nocode;
      // This would be preferable to using "if (global == null)" because it
      // would allow individual values to be overridden
      // in the sub level configuration. But it doesn't appear to work and I
      // don't have time to debug why.
      // baseConfig.global.setOnMap(globalVars);
      if (global == null) global = baseConfig.global;
      if (local == null) local = baseConfig.local;
      // you can't inherit dfnbase :)
      if (srcbase == null) srcbase = baseConfig.srcbase;
      if (cachebase == null) cachebase = baseConfig.cachebase;
      if (cgltemplate == null) cgltemplate = baseConfig.cgltemplate;
      if (querytemplate == null) querytemplate = baseConfig.querytemplate;
      if (module == null) module = baseConfig.module;
      if (schema == null) schema = baseConfig.schema;
      if (vssroot == null) vssroot = baseConfig.vssroot;
      if (vssdir == null) vssdir = baseConfig.vssdir;
      if (indextablespace == null)
        indextablespace = baseConfig.indextablespace;
      if (role == null) role = baseConfig.role;
      if (dbdriver == null) dbdriver = baseConfig.dbdriver;
      if (dburl == null) dburl = baseConfig.dburl;
      if (dbuser == null) dbuser = baseConfig.dbuser;
      if (dbpasswd == null) dbpasswd = baseConfig.dbpasswd;
      if (dbadapter == null) dbadapter = baseConfig.dbadaptername;
      // you can't inherit stampfile either
      if (logsql == null) logsql = baseConfig.logsql;
      if (!hacknobefore) hacknobefore = baseConfig.hacknobefore;
      strict.addAll(baseConfig.strict);
    }

    this.baseConfig = baseConfig;
    this.verboseCVS = verboseCVS;
    this.forcedrops = forcedrops;
    this.nodatabase = nodatabase;
    this.nocode = nocode == null ? false : nocode;
    this.global = global == null ? new Definition() : global;
    this.global.setOnMap(globalVars);
    this.local = local == null ? new Definition() : local;
    this.dfnbase = (DfnBase) notNull(dfnbase, "dfnbase", sr);
    this.srcbase = (File) notNull(srcbase, "srcbase", sr);
    this.cachebase = (File) notNull(cachebase, "cachebase", sr);
    this.cgltemplate = (File) notNull(cgltemplate, "cgltemplate", sr);
    this.querytemplate = (File) notNull(querytemplate, "querytemplate", sr);
    this.module = module;
    this.schema = schema;
    this.vssroot = vssroot;
    this.vssdir = vssdir;
    this.indextablespace = indextablespace;
    this.role = role;
    this.dbdriver = (String) notNull(dbdriver, "dbdriver", sr);
    this.dburl = (String) notNull(dburl, "dburl", sr);
    this.dbuser = dbuser;
    this.dbpasswd = dbpasswd;
    if (dbadapter == null) {
      Output.reportError(new FileLocation(cfgfile), "WARNING: 'dbadapter' not specified in config file, "
          + "assuming postgres");
      this.dbadaptername = "postgres";
    } else {
      this.dbadaptername = dbadapter;
    }
    this.dbadapter = DBAdapter.getInstance(this);
    this.stampfile = stampfile;
    this.logsql = logsql;
    this.hacknobefore = hacknobefore;

    if (baseConfig == null) {
      modified = cfgfile.lastModified();
    } else {
      modified = Math.max(baseConfig.modified, cfgfile.lastModified());
    }
  }

  public boolean strict(String str) {
    if (!legalStrict.contains(str))
      throw new RuntimeException(
          "Internal error: attempting to check for strict '" + str
              + "' but that's not a legal value");
    return strict.contains(str);
  }

  public boolean isNothingToDo() throws IOException {
    if (!stampfile.exists()) return false;
    
    long stampModified = stampfile.lastModified();
    
    BufferedReader reader = new BufferedReader(new FileReader(stampfile));
    try {
      String line;
      while ((line = reader.readLine()) != null) {
        File file = new File(line);
        if (!file.exists() || file.lastModified() > stampModified) {
          return false;
        }
      }
    } finally {
      reader.close();
    }
    return true;
  }
}
