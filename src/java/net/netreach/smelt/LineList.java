///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.smelt.LineList
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2000/09/12
// Description: Represent a list of lines in a configuration file.
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

// Collections classes used to implement the appropriate behavior.
import java.util.AbstractSequentialList;
import java.util.HashMap;
import java.util.Iterator;
import java.util.LinkedList;
import java.util.List;
import java.util.ListIterator;

/**
 * Represent a list of lines in a configuration file. The lines can be accessed
 * either by normal list iteration or through a map-like interface that returns
 * a line based on its first word, if any.
 * 
 * @author Stuart Ballard &lt;sballard@netreach.net&gt;
 */
public class LineList extends AbstractSequentialList {

  private LinkedList backing = new LinkedList();
  private HashMap byFirstWord = null;

  public List getLine(int i) {
    return (List) get(i);
  }

  public boolean add(Object o) {
    if (byFirstWord != null) {
      throw new IllegalArgumentException("LineList frozen");
    }
    List l = (List) o;
    return backing.add(l);
  }

  public List getOneByFirstWord(String fword) {
    if (byFirstWord == null) {
      buildFirstWordMap();
    }
    Object o = byFirstWord.get(fword);
    if (o instanceof List) {
      return (List) o;
    } else {
      return null;
    }
  }

  public List getAllByFirstWord(String fword) {
    if (byFirstWord == null) {
      buildFirstWordMap();
    }
    Object o = byFirstWord.get(fword);
    if (o == null) {
      return new LinkedList();
    } else if (o instanceof List) {
      List res = new LinkedList();
      res.add(o);
      return res;
    } else {
      return ((Multi) o).list;
    }
  }

  private class Multi {
    private List list = new LinkedList();

    Multi(List line) {
      list.add(line);
    }
  }

  private void buildFirstWordMap() {
    byFirstWord = new HashMap();
    Iterator li = iterator();
    while (li.hasNext()) {
      List line = (List) li.next();
      if (line.size() > 0) {
        Object w1 = line.get(0);
        if (w1 instanceof String) {
          if (!byFirstWord.containsKey(w1)) {
            byFirstWord.put(w1, line);
          } else {
            Object existing = byFirstWord.get(w1);
            if (existing instanceof List) {
              existing = new Multi((List) existing);
              byFirstWord.put(w1, existing);
            }
            ((Multi) existing).list.add(line);
          }
        }
      }
    }
  }

  public int size() {
    return backing.size();
  }

  public ListIterator listIterator(final int index) {
    return new ListIterator() {
      private final ListIterator li = backing.listIterator(index);

      public int nextIndex() {
        return li.nextIndex();
      }

      public int previousIndex() {
        return li.previousIndex();
      }

      public boolean hasNext() {
        return li.hasNext();
      }

      public boolean hasPrevious() {
        return li.hasPrevious();
      }

      public Object next() {
        return li.next();
      }

      public Object previous() {
        return li.previous();
      }

      public void remove() {
        throw new UnsupportedOperationException();
      }

      public void add(Object o) {
        throw new UnsupportedOperationException();
      }

      public void set(Object o) {
        throw new UnsupportedOperationException();
      }
    };
  }
}
