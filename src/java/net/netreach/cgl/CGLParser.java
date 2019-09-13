///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.cgl.CGLParser
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2001/05/10
// Description: Part of CGL interpreter
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

package net.netreach.cgl;

// Collections classes used in implementation.
import java.io.File;
import java.io.IOException;
import java.util.ArrayList;
import java.util.Collections;
import java.util.HashSet;
import java.util.Iterator;
import java.util.LinkedList;
import java.util.List;

import net.netreach.smelt.ParseException;
import net.netreach.smelt.SmeltReader;
import net.netreach.util.FileCache;
import net.netreach.util.FileLocatedException;
import net.netreach.util.FileLocation;
import net.netreach.util.Output;
import net.netreach.util.PathUtil;

public class CGLParser {

  static FileCache<Expr> templateCache = new FileCache<Expr>(new FileCache.Populator<Expr>() {

    public Expr get(File file) throws IOException, FileLocatedException {
      if (verbose) Output.println("CGL: Loading " + file + "... ");
      SmeltReader sr = new SmeltReader(file);
      sr.readToken();
      sr.skipToken("template");
      sr.checkSOB();
      Expr result = loadExpr(file, sr);
      sr.skipEOL();
      sr.checkEOF();
      if (verbose) Output.println("done.");
      return result;
    }
    
  });

  static FileCache<Definition> exportCache = new FileCache<Definition>(new FileCache.Populator<Definition>() {
    
    public Definition get(File file) throws IOException, FileLocatedException {
      if (verbose) Output.println("CGL: Importing " + file + "... ");
      SmeltReader sr = new SmeltReader(file);
      sr.readToken();
      sr.skipToken("export");
      Definition def = loadDefinition(file, sr);
      sr.skipEOL();
      sr.checkEOF();
      if (verbose) Output.println("done.");
      return def;
    }
  
  });

  static boolean verbose = false;

  public static void setVerbose(boolean verbose) {
    CGLParser.verbose = verbose;
  }

  public static java.util.Set getTouchedFiles() {
    HashSet result = new HashSet();
    result.addAll(exportCache.getFiles());
    result.addAll(templateCache.getFiles());
    return result;
  }

  public static void reset() {
    exportCache.reset();
    templateCache.reset();
    verbose = false;
    Outfile.reset();
  }

  public static Definition loadDefinition(File base, SmeltReader sr)
      throws IOException, ParseException {
    ParseContext pctx = new ParseContext();
    pctx.relativeTo = PathUtil.canonicalFile(base).getParentFile();
    pctx.timestamp = base.lastModified();
    sr.checkSOB();
    Definition def = parseDefineBlock(pctx, sr, true, true, true);
    sr.skipEOB();
    sr.checkEOL();
    return new TimestampedDefn(def, pctx.innerDeps, pctx.timestamp);
  }

  public static Expr loadExpr(File base, SmeltReader sr) throws IOException,
      ParseException {
    ParseContext pctx = new ParseContext();
    pctx.relativeTo = PathUtil.canonicalFile(base).getParentFile();
    pctx.timestamp = base.lastModified();
    Expr result = parseExpr(pctx, sr);
    sr.checkEOL();
    return new TimestampedExpr(result, pctx.innerDeps, pctx.timestamp);
  }

  public static Definition loadExport(File file) throws IOException,
      FileLocatedException {
    return exportCache.get(file);
  }

  public static Expr loadTemplate(File file) throws IOException, FileLocatedException {
    return templateCache.get(file);
  }

  static Expr parseChain(ParseContext pctx, SmeltReader sr)
      throws ParseException {
    return parseChain(pctx, sr, null, false, false);
  }

  static Expr parseChain(ParseContext pctx, SmeltReader sr, Expr sep,
      boolean wrapWhen, boolean wrapForAll) throws ParseException {
    sr.skipSOB();
    List exprs = new ArrayList();
    List whenDeps = new ArrayList();
    When currWhen = null;
    while (!sr.wasEOB()) {
      Expr res = parseAction(pctx, sr, true, sep != null, currWhen, whenDeps);
      if (res != null) {
        exprs.add(res);
      } else {
        res = currWhen.elseVal;
      }
      currWhen = res instanceof When ? (When) res : null;
      if (currWhen == null) whenDeps.clear();
      sr.skipEOL();
    }
    sr.checkEOB();
    if (exprs.isEmpty()) {
      return new Literal(null);
    } else if (exprs.size() == 1
        && (sep == null || !(exprs.get(0) instanceof ForAll))) {
      Expr res = (Expr) exprs.get(0);
      if ((wrapWhen && res instanceof When)
          || (wrapForAll && res instanceof ForAll)) {
        res = new Identity(res);
      }
      return res;
    } else {
      return new Chain(exprs, sep);
    }
  }

  static Expr parseExpr(ParseContext pctx, SmeltReader sr)
      throws ParseException {
    return parseExpr(pctx, sr, false, false);
  }

  static Expr parseExpr(ParseContext pctx, SmeltReader sr, boolean wrapWhen,
      boolean wrapForAll) throws ParseException {
    if (sr.wasSOB()) {
      Expr res = parseChain(pctx, sr, null, wrapWhen, wrapForAll);
      sr.skipEOB();
      return res;
    } else if (sr.wasString()) {
      return parseLiteral(pctx, sr);
    } else {
      throw new ParseException(sr, "Expected string or start of block, found "
          + sr.lastToString());
    }
  }

  static Expr parseAction(ParseContext pctx, SmeltReader sr)
      throws ParseException {
    return parseAction(pctx, sr, false, false, null, null);
  }

  static Expr parseAction(ParseContext pctx, SmeltReader sr, boolean wrapWhen,
      boolean wrapForAll, When currWhen, List whenDeps) throws ParseException {
    if (sr.wasEOL()) {
      throw new ParseException(sr, "Null statement");
    } else if (sr.wasSOB()) {
      Expr res = parseChain(pctx, sr, null, wrapWhen, wrapForAll);
      sr.skipEOB();
      sr.checkEOL();
      return res;
    } else if (sr.wasString()) {

      // Since we can't know whether it's a keyword or a literal until we read
      // the next token (to find if it's an EOL or something else), we must
      // prepare for both contingencies. Fortunately, parseLiteral is cheap if
      // it actually is a keyword, because all the keywords are short and don't
      // contain any dollar signs.
      String keyword = sr.checkString();
      Expr res = parseLiteral(pctx, sr);
      if (sr.wasEOL()) {
        return res;
      } else {

        // Must be a keyword...

        if ("sep".equals(keyword)) {
          Expr sep = parseExpr(pctx, sr);
          if (sr.wasSOB()) {
            res = parseChain(pctx, sr, sep, false, false);
            sr.skipEOB();
          } else {
            Expr body = parseAction(pctx, sr);
            if (body instanceof ForAll) {
              List exprs = Collections.singletonList(body);
              res = new Chain(exprs, sep);
            } else {
              throw new ParseException(sr, "Sep must take forall or {...}");
            }
          }

        } else if ("forall".equals(keyword)) {

          // Save original inner and active deps.
          List ideps = pctx.innerDeps;
          List adeps = pctx.activeDeps;
          pctx.innerDeps = new ArrayList();
          pctx.activeDeps = new ArrayList(adeps);

          // Parse list value and add deps of that to active deps for body.
          Expr listExpr = parseExpr(pctx, sr);
          pctx.activeDeps.addAll(pctx.innerDeps);
          ideps.addAll(pctx.innerDeps);
          pctx.innerDeps = ideps;

          // Parse the body and restore active-deps afterwards.
          res = new ForAll(listExpr, parseAction(pctx, sr));
          pctx.activeDeps = adeps;

        } else if ("define".equals(keyword)) {
          res = parseDefine(pctx, sr);

        } else if ("list".equals(keyword)) {
          List ldes = new LinkedList();
          sr.skipSOB();
          while (!sr.wasEOB()) {
            ldes.add(parseLDefn(pctx, sr));
            sr.skipEOL();
          }
          sr.skipEOB();
          res = new ListExpr(ldes);

        } else if ("anyof".equals(keyword)) {
          List exprs = new LinkedList();
          while (!sr.wasEOL()) {
            exprs.add(parseExpr(pctx, sr));
          }
          res = new AnyAllOf(exprs, true);

        } else if ("allof".equals(keyword)) {
          List exprs = new LinkedList();
          while (!sr.wasEOL()) {
            exprs.add(parseExpr(pctx, sr));
          }
          res = new AnyAllOf(exprs, false);

        } else if ("when".equals(keyword)) {

          // Save original inner and active deps.
          List ideps = pctx.innerDeps;
          List adeps = pctx.activeDeps;
          pctx.innerDeps = new ArrayList();
          pctx.activeDeps = new ArrayList(adeps);

          // Parse condition expr and add deps of that to active deps for body.
          Expr condition = parseExpr(pctx, sr);
          pctx.activeDeps.addAll(pctx.innerDeps);
          ideps.addAll(pctx.innerDeps);
          if (whenDeps != null) {
            if (currWhen != null) whenDeps.clear();
            whenDeps.addAll(pctx.innerDeps);
          }
          pctx.innerDeps = ideps;

          // Parse when expression and restore active deps afterwards.
          res = new When(condition, parseAction(pctx, sr));
          pctx.activeDeps = adeps;

        } else if ("else".equals(keyword)) {
          if (currWhen == null) {
            throw new ParseException(sr, "Else not permitted here");
          }
          List adeps = pctx.activeDeps;
          pctx.activeDeps = new ArrayList(adeps);
          pctx.activeDeps.addAll(whenDeps);
          currWhen.elseVal = parseAction(pctx, sr, false, false, null, whenDeps);
          pctx.activeDeps = adeps;
          res = null;

        } else if ("subst".equals(keyword)) {
          res = new Subst(parseExpr(pctx, sr), parseExpr(pctx, sr),
              parseAction(pctx, sr));

        } else if ("trans".equals(keyword)) {
          res = new Trans(sr.skipString(), sr.skipString(), parseAction(pctx,
              sr));

        } else if ("lcase".equals(keyword)) {
          res = new LCase(parseAction(pctx, sr));

        } else if ("ucase".equals(keyword)) {
          res = new UCase(parseAction(pctx, sr));

        } else if ("initcap".equals(keyword)) {
          res = new InitCap(parseAction(pctx, sr));

        } else if ("intercap".equals(keyword)) {
          res = new InterCap(sr.getLastTokenLocation(), sr.skipString(), parseAction(pctx,
              sr));

        } else if ("eq".equals(keyword)) {
          res = new Eq(parseExpr(pctx, sr), parseExpr(pctx, sr));

        } else if ("gt".equals(keyword)) {
          res = new Cmp(1, false, parseExpr(pctx, sr), parseExpr(pctx, sr));

        } else if ("ge".equals(keyword)) {
          res = new Cmp(1, true, parseExpr(pctx, sr), parseExpr(pctx, sr));

        } else if ("lt".equals(keyword)) {
          res = new Cmp(-1, false, parseExpr(pctx, sr), parseExpr(pctx, sr));

        } else if ("le".equals(keyword)) {
          res = new Cmp(-1, true, parseExpr(pctx, sr), parseExpr(pctx, sr));

        } else if ("ne".equals(keyword)) {
          res = new Ne(parseExpr(pctx, sr), parseExpr(pctx, sr));

        } else if ("plus".equals(keyword)) {
          res = new Plus(parseExpr(pctx, sr), parseExpr(pctx, sr));

        } else if ("minus".equals(keyword)) {
          res = new Minus(parseExpr(pctx, sr), parseExpr(pctx, sr));

        } else if ("times".equals(keyword)) {
          res = new Times(parseExpr(pctx, sr), parseExpr(pctx, sr));

        } else if ("divide".equals(keyword)) {
          res = new Divide(parseExpr(pctx, sr), parseExpr(pctx, sr));

        } else if ("not".equals(keyword)) {
          res = new Not(parseAction(pctx, sr));

        } else if ("defined".equals(keyword)) {
          res = new Defined(sr.skipString());

        } else if ("default".equals(keyword)) {
          res = new Default(sr.skipString(), sr.wasEOL() ? new Literal(null)
              : parseAction(pctx, sr));

        } else if ("length".equals(keyword)) {
          res = new Length(parseAction(pctx, sr));

        } else if ("str".equals(keyword)) {
          res = new Str(sr.getLastTokenLocation(), parseAction(pctx, sr));

        } else if ("int".equals(keyword)) {
          res = new Int(parseAction(pctx, sr));

        } else if ("box".equals(keyword)) {
          String name = sr.skipString();

          // Save original inner and active deps.
          List ideps = pctx.innerDeps;
          List adeps = pctx.activeDeps;
          pctx.innerDeps = new ArrayList();
          pctx.activeDeps = new ArrayList(adeps);

          // Parse initial value and add deps of that to active deps for body.
          Expr initValue = parseExpr(pctx, sr);
          pctx.activeDeps.addAll(pctx.innerDeps);
          ideps.addAll(pctx.innerDeps);
          pctx.innerDeps = ideps;

          // Parse body and restore active deps afterwards.
          pctx.nestedBoxes++;
          res = new Box(name, initValue, parseAction(pctx, sr));
          pctx.nestedBoxes--;
          pctx.activeDeps = adeps;

        } else if ("whatsin".equals(keyword)) {
          res = new WhatsIn(sr.skipString());

        } else if ("assign".equals(keyword)) {
          res = new Assign(sr.skipString(), parseAction(pctx, sr));

        } else if ("outfile".equals(keyword)
            || "paranoid-outfile".equals(keyword)) {

          // Save previous deps and active-deps, and prepare new ones.
          List ideps = pctx.innerDeps;
          List adeps = pctx.activeDeps;
          pctx.innerDeps = new ArrayList();
          pctx.activeDeps = new ArrayList(adeps);

          // Parse the filename portion of the statement. Inner deps here are
          // active in the body, and also need to be added to inner dependencies
          // for returning to our caller.
          Expr filename = parseExpr(pctx, sr);
          pctx.activeDeps.addAll(pctx.innerDeps);
          ideps.addAll(pctx.innerDeps);

          // Parse the body portion of the statement, with a fresh innerDeps.
          // Thus only the innerDeps from inside the body are passed to the
          // Outfile constructor. Again, these must be returned to our caller.
          pctx.innerDeps = new ArrayList();
          Expr body = parseAction(pctx, sr);
          res = new Outfile(pctx.timestamp, pctx.innerDeps, pctx.activeDeps,
              filename, body);
          ((Outfile) res).paranoid = "paranoid-outfile".equals(keyword);
          ideps.addAll(pctx.innerDeps);

          // Restore active- and inner- deps.
          pctx.activeDeps = adeps;
          pctx.innerDeps = ideps;

        } else if ("eval".equals(keyword)) {
          if (pctx.nestedBoxes > 0) {
            throw new ParseException(sr, "'eval' not permitted here");
          }
          res = new Eval(pctx.relativeTo, pctx.activeDeps, pctx.timestamp,
              parseAction(pctx, sr));
          pctx.innerDeps.add(res);

        } else if ("readfile".equals(keyword)) {
          if (pctx.nestedBoxes > 0) {
            throw new ParseException(sr, "'readfile' not permitted here");
          }
          res = new ReadFile(parseAction(pctx, sr));

        } else {
          throw new ParseException(sr, "Unknown keyword '" + keyword + "'");
        }
        sr.checkEOL();
        return res;
      }
    } else {
      throw new ParseException(sr, "Illegal token " + sr.lastToString());
    }
  }

  static Expr parseLiteral(ParseContext pctx, SmeltReader sr)
      throws ParseException {
    String code = sr.checkString();
    int pos = 0;
    if (code.indexOf('$') == -1) {
      sr.skipString();
      return new Literal(code);
    }
    StringBuffer sb = new StringBuffer();
    List results = new ArrayList();
    while (pos < code.length()) {
      int newpos = code.indexOf('$', pos + 1);
      if (code.charAt(pos) == '$') {
        if (newpos < 0) {
          throw new ParseException(sr, "Missing $", pos, code.length());
        } else if (newpos == pos + 1) {
          sb.append('$');
        } else {
          if (sb.length() > 0) {
            results.add(new Literal(sb.toString()));
            sb = new StringBuffer();
          }
          FileLocation loc = sr.getLocForStringRange(pos, newpos);
          results.add(new Str(loc, new Var(loc, code.substring(pos + 1, newpos))));
        }
        newpos++;
      } else {
        if (newpos < 0) newpos = code.length();
        sb.append(code.substring(pos, newpos));
      }
      pos = newpos;
    }
    if (results.isEmpty() || sb.length() > 0) {
      results.add(new Literal(sb.toString()));
    }
    sr.skipString();
    if (results.size() == 1) {
      Expr result = (Expr) results.get(0);
      if (result instanceof Str) {
        return ((Str) result).value;
      } else {
        return result;
      }
    } else {
      return new Chain(results);
    }
  }

  static Definition parseDefineBlock(ParseContext pctx, SmeltReader sr)
      throws ParseException {
    return parseDefineBlock(pctx, sr, true, true, true);
  }

  static Definition parseDefineBlock(ParseContext pctx, SmeltReader sr,
      boolean allowImport, boolean allowDeclare, boolean allowInside)
      throws ParseException {
    sr.skipSOB();

    // Save original active deps.
    List adeps = pctx.activeDeps;
    pctx.activeDeps = new ArrayList(adeps);

    Definition defn = new Definition();
    while (!sr.wasEOB()) {
      if (sr.wasEOL()) {
        throw new ParseException(sr, "Null define");
      }
      String keyword = sr.checkString();

      if ("extract".equals(keyword)) {
        sr.skipString();
        if (sr.wasString()) {
          FileLocation loc = sr.getLastTokenLocation();
          String name = sr.skipString();
          defn.elems.add(new Set(name, new Var(loc, name)));
          sr.checkEOL();
        } else if (sr.wasSOB()) {
          sr.skipSOB();
          while (!sr.wasEOB()) {
            FileLocation loc = sr.getLastTokenLocation();
            String name = sr.skipString();
            defn.elems.add(new Set(name, new Var(loc, name)));
            sr.skipEOL();
          }
          sr.skipEOB();
        } else {
          throw new ParseException(sr, "Expected a string or block, found "
              + sr.lastToString());
        }
      } else if ("set".equals(keyword)) {
        sr.skipString();

        // Save original inner deps;
        List ideps = pctx.innerDeps;
        pctx.innerDeps = new ArrayList();

        // Parse set value and add deps of that to active deps.
        defn.elems.add(new Set(sr.skipString(), parseAction(pctx, sr)));
        pctx.activeDeps.addAll(pctx.innerDeps);
        ideps.addAll(pctx.innerDeps);
        pctx.innerDeps = ideps;

      } else if ("use".equals(keyword)) {
        sr.skipString();
        defn.elems.add(new Use(sr.skipString()));
      } else if ("declare".equals(keyword)) {
        if (!allowDeclare) {
          throw new ParseException(sr, "'declare' not permitted here");
        }
        sr.skipString();
        String name = sr.skipString();
        List givens = new LinkedList();
        if (sr.wasToken("given")) {
          sr.skipToken("given");
          sr.skipSOB();
          while (!sr.wasEOB()) {
            givens.add(sr.skipString());
            sr.skipEOL();
          }
          sr.skipEOB();
        }

        // Save original inner deps;
        List ideps = pctx.innerDeps;
        pctx.innerDeps = new ArrayList();

        // Parse set value and add deps of that to active deps.
        defn.elems.add(new Declare(name, givens, parseDefineBlock(pctx, sr,
            false, false, true)));
        pctx.activeDeps.addAll(pctx.innerDeps);
        ideps.addAll(pctx.innerDeps);
        pctx.innerDeps = ideps;

        sr.skipEOB();
      } else if ("inside".equals(keyword)) {
        if (!allowInside) {
          throw new ParseException(sr, "'inside' not permitted here");
        }
        sr.skipString();
        List names;
        if (sr.wasString()) {
          names = Collections.singletonList(new NameTree(sr.skipString()));
        } else if (sr.wasSOB()) {
          names = parseNameTree(sr, true);
        } else {
          throw new ParseException(sr, "Expected a string or block, found "
              + sr.lastToString());
        }
        List givens = new LinkedList();
        if (sr.wasToken("given")) {
          sr.skipToken("given");
          sr.skipSOB();
          while (!sr.wasEOB()) {
            givens.add(sr.skipString());
            sr.skipEOL();
          }
          sr.skipEOB();
        }
        // Save original inner deps;
        List ideps = pctx.innerDeps;
        pctx.innerDeps = new ArrayList();

        // Parse set value and add deps of that to active deps.
        Definition defns = parseDefineBlock(pctx, sr, false, false, true);
        pctx.activeDeps.addAll(pctx.innerDeps);
        ideps.addAll(pctx.innerDeps);
        pctx.innerDeps = ideps;

        sr.skipEOB();
        addInsides(names, defn.elems, givens, defns);
      } else if ("import".equals(keyword)) {
        if (!allowImport || pctx.nestedBoxes > 0) {
          throw new ParseException(sr, "'import' not permitted here");
        }
        sr.skipString();
        Import imp = new Import(pctx.relativeTo, pctx.activeDeps,
            pctx.timestamp, parseAction(pctx, sr));
        pctx.innerDeps.add(imp);
        defn.elems.add(imp);

      } else {
        throw new ParseException(sr, "Unknown define keyword '" + keyword + "'");
      }
      sr.skipEOL();
    }
    pctx.activeDeps = adeps;
    return defn;
  }

  static Expr parseDefine(ParseContext pctx, SmeltReader sr)
      throws ParseException {
    Define def = new Define();

    // Save original inner and active deps.
    List ideps = pctx.innerDeps;
    List adeps = pctx.activeDeps;
    pctx.innerDeps = new ArrayList();
    pctx.activeDeps = new ArrayList(adeps);

    // Parse list value and add deps of that to active deps for body.
    def.elems = parseDefineBlock(pctx, sr, true, true, true);
    pctx.activeDeps.addAll(pctx.innerDeps);
    ideps.addAll(pctx.innerDeps);
    pctx.innerDeps = ideps;
    sr.skipEOB();

    // Parse the body and restore active-deps afterwards.
    def.body = parseAction(pctx, sr);
    pctx.activeDeps = adeps;

    return def;
  }

  static class NameTree {
    String name;
    List tree;

    NameTree(String name) {
      this.name = name;
    }

    NameTree(String name, List tree) {
      this.name = name;
      this.tree = tree;
    }

    public String toString() {
      StringBuffer sb = new StringBuffer(name);
      if (tree != null) {
        sb.append(' ');
        if (tree.size() != 1) sb.append('{');
        for (Iterator i = tree.iterator(); i.hasNext();) {
          sb.append(i.next());
          if (i.hasNext()) sb.append("; ");
        }
        if (tree.size() != 1) sb.append('}');
      }
      return sb.toString();
    }
  }

  static List parseNameTree(SmeltReader sr, boolean allowOne)
      throws ParseException {
    List into = new LinkedList();
    if (sr.wasString()) {
      NameTree nmtree = new NameTree(sr.skipString());
      if (!sr.wasEOL()) {
        nmtree.tree = parseNameTree(sr, false);
      }
      into.add(nmtree);
    } else if (sr.wasSOB()) {
      sr.skipSOB();
      int count = 0;
      while (!sr.wasEOB()) {
        into.addAll(parseNameTree(sr, false));
        sr.skipEOL();
        count++;
      }
      if (into.isEmpty()) {
        throw new ParseException(sr, "Illegal empty block");
      } else if (!allowOne && count == 1) {
        throw new ParseException(sr, "Illegal redundant braces around"
            + new NameTree("", into).toString());
      }
      sr.skipEOB();
    } else {
      throw new ParseException(sr, "Expected string or block, found "
          + sr.lastToString());
    }
    return into;
  }

  static void addInsides(List nms, List target, List givens, Definition defns) {
    for (Iterator i = nms.iterator(); i.hasNext();) {
      NameTree name = (NameTree) i.next();
      Definition ndefns = defns;
      List ngivens = givens;
      if (name.tree != null) {
        List igivens = new LinkedList();
        for (Iterator j = name.tree.iterator(); j.hasNext();) {
          igivens.add(((NameTree) j.next()).name);
        }
        Definition idefns = new Definition();
        addInsides(name.tree, idefns.elems, givens, defns);
        ndefns = idefns;
        ngivens = igivens;
      }
      target.add(new Inside(name.name, ngivens, ndefns));
    }
  }

  static LDefnElem parseLDefn(ParseContext pctx, SmeltReader sr)
      throws ParseException {
    LDefnElem res;
    if (sr.wasSOB()) {
      res = new BasicLDefn(parseDefineBlock(pctx, sr, false, false, false));
      sr.skipEOB();
    } else if (sr.wasToken("list")) {
      sr.skipToken("list");
      List ldes = new LinkedList();
      sr.skipSOB();
      while (!sr.wasEOB()) {
        ldes.add(parseLDefn(pctx, sr));
        sr.skipEOL();
      }
      sr.skipEOB();
      res = new ListLDefn(ldes);
    } else if (sr.wasToken("sep")) {
      sr.skipToken("sep");

      // Save original inner and active deps.
      List ideps = pctx.innerDeps;
      List adeps = pctx.activeDeps;
      pctx.innerDeps = new ArrayList();
      pctx.activeDeps = new ArrayList(adeps);

      // Parse sep expression and add deps to active deps for body.
      LDefnElem sep = parseLDefn(pctx, sr);
      pctx.activeDeps.addAll(pctx.innerDeps);
      ideps.addAll(pctx.innerDeps);
      pctx.innerDeps = ideps;

      // Parse list-sep body and restore original active deps afterwards.
      List body = new LinkedList();
      if (sr.wasToken("forall")) {
        body.add(parseLDefn(pctx, sr));
      } else {
        sr.skipSOB();
        while (!sr.wasEOB()) {
          body.add(parseLDefn(pctx, sr));
          sr.skipEOL();
        }
        sr.skipEOB();
      }
      res = new SepLDefn(sep, body);
      pctx.activeDeps = adeps;

    } else if (sr.wasToken("when")) {
      sr.skipToken("when");

      // Save original inner and active deps.
      List ideps = pctx.innerDeps;
      List adeps = pctx.activeDeps;
      pctx.innerDeps = new ArrayList();
      pctx.activeDeps = new ArrayList(adeps);

      // Parse condition expression and add deps to active deps for body.
      Expr condition = parseExpr(pctx, sr);
      pctx.activeDeps.addAll(pctx.innerDeps);
      ideps.addAll(pctx.innerDeps);
      pctx.innerDeps = ideps;

      // Parse list-when and restore original active deps afterwards.
      res = new WhenLDefn(condition, parseLDefn(pctx, sr));
      pctx.activeDeps = adeps;

    } else if (sr.wasToken("forall")) {
      sr.skipToken("forall");

      // Save original inner and active deps.
      List ideps = pctx.innerDeps;
      List adeps = pctx.activeDeps;
      pctx.innerDeps = new ArrayList();
      pctx.activeDeps = new ArrayList(adeps);

      // Parse list expression and add deps to active deps for body.
      Expr listExpr = parseExpr(pctx, sr);
      pctx.activeDeps.addAll(pctx.innerDeps);
      ideps.addAll(pctx.innerDeps);
      pctx.innerDeps = ideps;

      // Parse list-forall and restore original active deps afterwards.
      res = new ForAllLDefn(listExpr, parseLDefn(pctx, sr));
      pctx.activeDeps = adeps;

    } else if (sr.wasString()) {
      String var = sr.skipString();
      if (var.startsWith("$") && var.indexOf('$', 1) == var.length() - 1) {
        res = new VarLDefn(var.substring(1, var.length() - 1));
      } else {
        throw new ParseException(sr, "Illegal syntax in 'list'");
      }
      ;

    } else {
      throw new ParseException(sr, "Illegal syntax in 'list'");
    }
    return res;
  }
}
