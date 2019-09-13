///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.smelt.OMParser
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2001/08/17
// Description: Parses a Smelt file into lines and lists of lines.
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

package net.netreach.smelt;

// I/O classes used.
import java.io.File;
import java.io.IOException;
import java.io.Reader;
import java.util.ArrayList;
import java.util.List;

/**
 * Parses a Smelt file into lines and lists of lines. The OM stands for Object
 * Model, and refers to the parallel between this approach and the DOM used for
 * interpreting XML. Like the DOM, using this class is a tradeoff: the object
 * model approach is conceptually simpler and easier to program to, but it is
 * also much higher overhead due to the necessity of interpreting the entire
 * file and storing it as a data structure in memory. For Smelt, the alternative
 * approach is to use SmeltReader directly, which not only avoids this overhead
 * but also provides the ability to access the line and column numbers of each
 * token in the file, which is very useful for error messages.
 * 
 * @author Stuart Ballard &lt;sballard@netreach.net&gt;
 */
public class OMParser {

  private static final boolean DEBUG = false;

  public static List parseLine(File f) throws IOException, ParseException {
    return parseLine(new SmeltReader(f));
  }

  public static List parseLine(Reader in, String filename)
      throws ParseException {
    return parseLine(new SmeltReader(in, filename));
  }

  public static List parseLine(SmeltReader sr) throws ParseException {
    LineList l = parse(sr);
    if (l.isEmpty()) return null;
    if (l.size() != 1) {
      throw new ParseException(sr, "More than one line found in file");
    }
    return (List) l.get(0);
  }

  public static LineList parse(File f) throws IOException, ParseException {
    return parse(new SmeltReader(f));
  }

  public static LineList parse(Reader in, String filename)
      throws ParseException {
    return parse(new SmeltReader(in, filename));
  }

  public static LineList parse(SmeltReader sr) throws ParseException {
    sr.readToken();
    LineList l = new LineList();
    while (!sr.wasEOF()) {
      if (DEBUG) System.err.println("OMP: Parsing a toplevel line...");
      l.add(parseSingleLine(sr));
      if (DEBUG) System.err.println("OMP: Done a toplevel line.");
    }
    if (DEBUG) System.err.println("OMP: EOF reached.");
    return l;
  }

  public static List parseSingleLine(SmeltReader sr) throws ParseException {
    List l = new ArrayList();
    if (DEBUG) System.err.println("OMP: Parsing a line...");
    while (!sr.wasEOL()) {
      if (sr.wasSOB()) {
        l.add(parseBlock(sr));
      } else if (sr.wasString()) {
        if (DEBUG) System.err.println("OMP: Read a string.");
        l.add(sr.skipString());
      } else {
        throw new ParseException(sr, "Internal error: Illegal token in line: "
            + sr.lastToString());
      }
    }
    sr.skipEOL();
    if (DEBUG)
      System.err.println("OMP: Done a line. (next token " + sr.lastToString()
          + ")");
    return l;
  }

  public static LineList parseBlock(SmeltReader sr) throws ParseException {
    LineList l = new LineList();
    if (DEBUG) System.err.println("OMP: Parsing a block...");
    sr.skipSOB();
    while (!sr.wasEOB()) {
      l.add(parseSingleLine(sr));
    }
    sr.skipEOB();
    if (DEBUG)
      System.err.println("OMP: Done a block. (next token " + sr.lastToString()
          + ")");
    return l;
  }

  public static void main(String[] args) throws ParseException, IOException {
    for (int i = 0; i < args.length; i++) {
      System.out.println(parse(new File(args[i])).toString());
    }
  }
}
