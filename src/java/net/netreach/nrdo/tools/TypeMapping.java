///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.nrdo.tools.TypeMapping
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2002/08/27
// Description: Map types to allow multiple databases to share dfn files.
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
import java.util.ArrayList;
import java.util.Iterator;
import java.util.List;

/**
 * Map types to allow multiple databases to share dfn files.
 * 
 * @author Stuart Ballard &lt;sballard@netreach.net&gt;
 */
public class TypeMapping {
  public List entries = new ArrayList();

  public static class Entry {
    public String fromStart;
    public String fromEnd;
    public String toStart;
    public String toEnd;

    public Entry(String from, String to) {
      int fromPos = from.indexOf('*');
      int toPos = to.indexOf('*');
      if (fromPos >= 0) {
        if (from.indexOf('*', fromPos + 1) >= 0) {
          throw new RuntimeException("Typemaps can only contain a single '*': "
              + from);
        }
        if (toPos >= 0 && to.indexOf('*', toPos + 1) >= 0) {
          throw new RuntimeException("Typemaps can only contain a single '*': "
              + to);
        }
        fromStart = from.substring(0, fromPos);
        fromEnd = from.substring(fromPos + 1);
        if (toPos < 0) {
          toStart = to;
        } else {
          toStart = to.substring(0, toPos);
          toEnd = to.substring(toPos + 1);
        }
      } else {
        fromStart = from;
        toStart = to;
      }
    }

    public String map(String from) {
      if (fromEnd == null) {
        if (fromStart.equals(from)) return toStart;
      } else {
        if (from.length() >= fromStart.length() + fromEnd.length()
            && from.startsWith(fromStart) && from.endsWith(fromEnd)) {
          return toEnd == null ? toStart : (toStart
              + from.substring(fromStart.length(), from.length()
                  - fromEnd.length()) + toEnd);
        }
      }
      return null;
    }
  }

  public String map(String from) {
    for (Iterator i = entries.iterator(); i.hasNext();) {
      Entry ent = (Entry) i.next();
      String res = ent.map(from);
      if (res != null) return res;
    }
    return from;
  }
}
