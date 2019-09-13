///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.cgl.Scope
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
import java.util.AbstractMap;
import java.util.AbstractSet;
import java.util.Collections;
import java.util.HashMap;
import java.util.IdentityHashMap;
import java.util.Iterator;
import java.util.Map;
import java.util.NoSuchElementException;
import java.util.Set;

import net.netreach.util.Output;

public class Scope extends AbstractMap {

  private static final boolean DEBUG = false;

  private static IdentityHashMap allScopes;
  static {
    if (DEBUG) allScopes = new IdentityHashMap();
  }
  private static int instanceCount = 0;
  private static int nesting = 0;

  private Map backing;
  private Map front;
  private int size;
  private boolean frozen = false;
  private boolean nocache = false;

  public Scope() {
    this(Collections.EMPTY_MAP);
  }

  public Scope(Map backing) {
    this(backing, null);
  }

  public Scope(Map backing, Map front) {
    this.backing = backing;
    this.front = front != null ? front : new HashMap();
    size = backing.size();

    if (DEBUG) {
      debug("Scope " + debugToString(this) + " = new Scope("
          + debugToString(backing) + "," + debugToString(front) + ");");
      debugDone();
    }
  }

  void setBacking(Map backing) {
    if (DEBUG) {
      debug(debugToString(this) + ".setBacking(" + debugToString(backing)
          + ");");
      debugDone();
    }
    this.backing = backing;
  }

  void setFront(Map front) {
    if (DEBUG) {
      debug(debugToString(this) + ".setFront(" + debugToString(front) + ");");
      debugDone();
    }
    this.front = front;
  }

  Map getBacking() {
    return backing;
  }

  Map getFront() {
    return front;
  }

  void setNoCache(boolean nocache) {
    if (DEBUG) {
      debug(debugToString(this) + ".setNoCache(" + nocache + ");");
      debugDone();
    }
    this.nocache = nocache;
  }

  boolean getNoCache() {
    return nocache;
  }

  private void debug(String s) {
    if (DEBUG) {
      if (nesting == 0) Output.println(s);
      nesting++;
    }
  }

  private void debugDone() {
    if (DEBUG) {
      nesting--;
    }
  }

  private String debugToString(Object m) {
    if (!DEBUG) return null;
    if (m == null) return "null";
    if (allScopes.containsKey(m)) return (String) allScopes.get(m);

    String result;
    if (m instanceof Scope) {
      result = "scope" + ++instanceCount;
    } else if (m instanceof String) {
      result = "\"" + m + "\"";
    } else if (m instanceof Number || m instanceof Boolean
        || m instanceof Character) {
      result = "new " + m.getClass() + "(" + m + ")";
    } else {
      result = m.getClass().toString().toLowerCase() + ++instanceCount;
      result = result.substring(result.lastIndexOf(".") + 1);
      result = result.substring(result.lastIndexOf("$") + 1);
      Output.println("" + m.getClass() + " " + result + "= new " + m.getClass()
          + "(" + debugStringInternal(m) + ");");
    }
    allScopes.put(m, result);
    return result;
  }

  private String debugStringInternal(Object m) {
    StringBuffer sb = new StringBuffer();
    if (m instanceof net.netreach.util.LazyMap) {
    } else if (m instanceof Map) {
      Map map = (Map) m;
      for (Iterator i = map.entrySet().iterator(); i.hasNext();) {
        if (sb.length() > 0) sb.append(", ");
        Map.Entry e = (Map.Entry) i.next();
        sb
            .append(debugToString(e.getKey()) + "="
                + debugToString(e.getValue()));
      }
    } else if (m instanceof java.util.Collection) {
      java.util.Collection coll = (java.util.Collection) m;
      for (Iterator i = coll.iterator(); i.hasNext();) {
        if (sb.length() > 0) sb.append(", ");
        sb.append(debugToString(i.next()));
      }
    } else {
      sb.append(m.toString());
    }
    return sb.toString();
  }

  public Object get(Object key) {
    if (DEBUG)
      debug(debugToString(this) + ".get(" + debugToString(key) + ");");
    try {
      Object o = front.get(key);
      if (o != null || front.containsKey(key)) return o;
      Object o2 = backing.get(key);
      if (!nocache && (o2 != null || backing.containsKey(key))) {
        front.put(key, o2);
      }
      return o2;
    } finally {
      if (DEBUG) debugDone();
    }
  }

  public boolean containsKey(Object key) {
    return front.containsKey(key) || backing.containsKey(key);
  }

  // This doesn't honor the full specification of put() because it doesn't
  // necessarily return the prior value accurately. This is because get() on
  // some possible backing maps (eg TableDef.toMap()) can have side effects
  // that we want to avoid. The return value of put() is hardly ever used,
  // anyway.
  public Object put(Object key, Object value) {
    if (DEBUG)
      debug(debugToString(this) + ".put(" + debugToString(key) + ", "
          + debugToString(value) + ");");
    if (frozen) throw new UnsupportedOperationException("Scope is frozen");
    Object o = null;
    if (front.containsKey(key)) {
      o = front.get(key);
    } else {
      size++;
    }
    front.put(key, value);
    if (DEBUG) debugDone();
    return o;
  }

  public int size() {
    return size;
  }

  public void freeze() {
    frozen = true;
  }

  public Set entrySet() {
    return new AbstractSet() {
      public boolean contains(Object key) {
        return containsKey(key);
      }

      public int size() {
        return size;
      }

      public Iterator iterator() {
        return new Iterator() {
          boolean onBacking = false;
          Iterator i = front.entrySet().iterator();
          boolean hasNext = true;
          boolean inited = false;

          private void init() {
            if (!inited) {
              inited = true;
              advance();
            }
          }

          Object next;

          private void advance() {
            if (!hasNext) throw new NoSuchElementException();
            boolean done = false;
            while (!done) {
              if (i.hasNext()) {
                next = i.next();
                done = !(onBacking && front.containsKey(((Map.Entry) next)
                    .getKey()));
              } else {
                if (onBacking) {
                  hasNext = false;
                  done = true;
                } else {
                  i = backing.entrySet().iterator();
                  onBacking = true;
                }
              }
            }
          }

          public boolean hasNext() {
            init();
            return hasNext;
          }

          public Object next() {
            init();
            Object o = next;
            advance();
            return o;
          }

          public void remove() {
            throw new UnsupportedOperationException();
          }
        };
      }
    };
  }

  private static String toString(Map m) {
    return (m instanceof Scope ? (Object) m : (Object) m.entrySet()).toString();
  }

  public String toString() {
    return "[ " + toString(front) + " // " + toString(backing) + " ]";
  }
}
