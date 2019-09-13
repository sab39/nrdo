///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.smelt.LineListReader
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2001/12/26
// Description: Create a SmeltReader from a LineList.
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
import java.io.IOException;
import java.util.Iterator;
import java.util.List;

public class LineListReader extends SmeltReader {

  private static final boolean DEBUG = false;

  /**
   * Create a SmeltReader based on a LineList. This is useful for applications
   * using the old LineList-based API if they need to call APIs (such as CGL)
   * that require SmeltReaders to function.
   * 
   * @param ll
   *          the LineList to interpret.
   */
  public LineListReader(LineList ll) throws IOException, ParseException {
    it = new IterEntry();
    it.lli = ll.iterator();
  }

  protected int state = SOB_STATE;
  protected static final int SOB_STATE = 1;
  protected static final int EOB_STATE = 2;
  protected static final int EOF_STATE = 3;
  protected static final int LINE_STATE = 4;
  protected static final int LINELIST_STATE = 5;

  protected class IterEntry {
    Iterator lli;
    Iterator li;
    IterEntry next;
  }

  IterEntry it;

  /**
   * Read a single token from the smelt stream.
   * 
   * @return EOF if the end of file was encountered, SOB if an open-brace '{'
   *         was encountered, EOB if a close-brace '}' was encountered, EOL if a
   *         semicolon ';' was encountered, or a String object containing the
   *         single word that was encountered. An EOL is guaranteed to be
   *         returned before every EOB or EOF, unless the block is completely
   *         empty.
   * @exception ParseException
   *              if mismatched braces, an unterminated string, or any other raw
   *              syntactical error was found in the stream.
   */
  public Object readToken() throws ParseException {
    lastToken = null;
    while (lastToken == null) {
      switch (state) {
      case SOB_STATE:
        lastToken = SOB;
        state = LINELIST_STATE;
        break;
      case LINELIST_STATE:
        if (it.lli.hasNext()) {
          List line = (List) it.lli.next();
          it.li = line.iterator();
          state = LINE_STATE;
        } else {
          lastToken = EOB;
          state = EOB_STATE;
        }
        break;
      case LINE_STATE:
        if (it.li.hasNext()) {
          Object o = it.li.next();
          if (o instanceof LineList) {
            IterEntry newIt = new IterEntry();
            newIt.lli = ((LineList) o).iterator();
            newIt.next = it;
            it = newIt;
            state = SOB_STATE;
          } else if (o instanceof String) {
            lastToken = o;
          }
        } else {
          lastToken = EOL;
          state = LINELIST_STATE;
        }
        break;
      case EOB_STATE:
        state = LINE_STATE;
        it = it.next;
        if (it == null) {
          lastToken = EOL;
          state = EOF_STATE;
        }
        break;
      case EOF_STATE:
        lastToken = EOF;
        break;
      default:
        throw new IllegalStateException("Internal error: state = " + state);
      }
    }
    if (DEBUG) System.err.println("LLR: found " + lastToString());
    return lastToken;
  }
}
