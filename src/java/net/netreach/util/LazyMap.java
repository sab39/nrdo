///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.util.misc.LazyMap
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2001/07/31
// Description: A map that can be lazily filled in over time.
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

// Collections classes used in implementation.
import java.util.AbstractMap;
import java.util.AbstractSet;
import java.util.HashMap;
import java.util.Iterator;
import java.util.Map;
import java.util.Set;

public abstract class LazyMap extends AbstractMap {
  protected Map backing = new HashMap();

  protected abstract Set getAvailSet();

  protected abstract void fillIn(Object key);

  public Object get(Object key) {
    if (!backing.containsKey(key)) {
      fillIn(key);
    }
    return backing.get(key);
  }

  public boolean containsKey(Object key) {
    if (getAvailSet().contains(key) || backing.containsKey(key)) {
      return true;
    } else {
      fillIn(key);
      return backing.containsKey(key);
    }
  }

  public Object put(Object key, Object value) {
    Object o = (getAvailSet().contains(key) ? this : backing).get(key);
    backing.put(key, value);
    return o;
  }

  public int size() {
    return getAvailSet().size();
  }

  public Set entrySet() {
    return new AbstractSet() {
      public boolean contains(Object key) {
        return containsKey(key);
      }

      public int size() {
        return getAvailSet().size();
      }

      public Iterator iterator() {
        return new Iterator() {
          Iterator i = getAvailSet().iterator();

          public boolean hasNext() {
            return i.hasNext();
          }

          public Object next() {
            final Object o = i.next();
            return new Map.Entry() {
              public Object getKey() {
                return o;
              }

              public Object getValue() {
                return get(o);
              }

              public Object setValue(Object val) {
                return put(o, val);
              }

              public String toString() {
                return "" + getKey() + "=" + getValue();
              }
            };
          }

          public void remove() {
            throw new UnsupportedOperationException();
          }
        };
      }
    };
  }
}
