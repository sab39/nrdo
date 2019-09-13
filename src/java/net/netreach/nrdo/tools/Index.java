///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.nrdo.tools.Index
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2000/10/19
// Description: A partially-ordered list of fields that can be used as an index.
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
import java.util.AbstractList;
import java.util.ArrayList;
import java.util.Collection;
import java.util.HashMap;
import java.util.Iterator;
import java.util.LinkedList;
import java.util.List;
import java.util.ListIterator;
import java.util.Map;

import net.netreach.util.Mappable;
import net.netreach.util.MappedList;
import net.netreach.util.Output;

/**
 * A partially-ordered list of fields that can be used as an index.
 * 
 * @author Stuart Ballard &lt;sballard@netreach.net&gt;
 */
public class Index extends AbstractList implements Mappable {

  List parts;
  int size = 0;
  String name;
  boolean isUnique;

  public Index() {
    parts = new ArrayList();
    parts.add(new ArrayList());
  }

  public Index(List l) {
    this(l, false);
  }

  public Index(List l, boolean ordered) {
    this();
    for (Iterator li = l.iterator(); li.hasNext();) {
      add(li.next());
      if (ordered) cut();
    }
  }

  public String toString() {
    String str = name + "<";
    for (Iterator li = parts.iterator(); li.hasNext();) {
      str += li.next();
    }
    return str + ">";
  }

  public int size() {
    return size;
  }

  public Object get(int i) {
    int p = 0;
    for (Iterator li = parts.iterator(); li.hasNext();) {
      List part = (List) li.next();
      if (p <= i && i < p + part.size()) {
        return part.get(i - p);
      }
      p += part.size();
    }
    throw new IndexOutOfBoundsException();
  }

  private Map asMap;

  public Map toMap() {
    if (asMap == null) {
      asMap = new HashMap();
      asMap.put("name", name);
      asMap.put("fields", new MappedList(this));
      asMap.put("unique", new Boolean(isUnique));
      asMap.put("pkey", new Boolean(name.startsWith("pk")));
    }
    return asMap;
  }

  public Index makeSuper(Index ix) {
    Index nix = new Index();
    Iterator ali = parts.iterator();
    Iterator bli = ix.parts.iterator();
    List a = null;
    List b = null;
    while ((!empty(a) || ali.hasNext()) && (!empty(b) || bli.hasNext())) {
      if (empty(a)) {
        nix.cut();
        a = new LinkedList((List) ali.next());
      }
      if (empty(b)) {
        nix.cut();
        b = new LinkedList((List) bli.next());
      }
      for (Iterator li = a.iterator(); li.hasNext();) {
        Object o = li.next();
        if (b.contains(o)) {
          li.remove();
          b.remove(o);
          nix.add(o);
        }
      }
      if (!a.isEmpty() && !b.isEmpty()) return null;
    }
    nix.cut();
    if (!ix.isUnique && !a.isEmpty()) nix.addAll(a);
    if (!isUnique && !b.isEmpty()) nix.addAll(b);
    while (!ix.isUnique && ali.hasNext()) {
      nix.cut();
      nix.addAll((List) ali.next());
    }
    while (!isUnique && bli.hasNext()) {
      nix.cut();
      nix.addAll((List) bli.next());
    }
    if (nix.size == size)
      nix.name = name;
    else
      nix.name = ix.name;
    nix.isUnique = isUnique || ix.isUnique;
    return nix;
  }

  private boolean empty(Collection c) {
    return c == null || c.isEmpty();
  }

  public boolean contains(Object o) {
    for (Iterator li = parts.iterator(); li.hasNext();) {
      List part = (List) li.next();
      if (part.contains(o)) return true;
    }
    return false;
  }

  public boolean add(Object o) {
    List last = (List) parts.get(parts.size() - 1);
    if (last.contains(o)) return false;
    if (contains(o)) {
      throw new RuntimeException("Cannot have a field in two separate parts");
    }
    size++;
    return last.add(o);
  }

  public boolean addAll(Collection c) {
    List last = (List) parts.get(parts.size() - 1);
    size += c.size();
    return last.addAll(c);
  }

  public void cut() {
    List last = (List) parts.get(parts.size() - 1);
    if (!last.isEmpty()) {
      parts.add(new ArrayList());
    }
  }

  public static List trimUniques(List uniques) {
    // Output.println("Starting uniques: " + uniques);
    List nUniques = new ArrayList();
    for (Iterator li = uniques.iterator(); li.hasNext();) {
      Index ix = (Index) li.next();
      boolean skip = false;
      for (ListIterator oli = nUniques.listIterator(); !skip && oli.hasNext();) {
        Index oix = (Index) oli.next();
        Index nix = oix.makeSuper(ix);
        if (nix != null) {
          if (oix.size() >= ix.size()) {
            oli.set(nix);
            skip = true;
          } else {
            oli.remove();
            ix = nix;
          }
        }
      }
      if (!skip) nUniques.add(ix);
    }
    return nUniques;
  }

  public static List trimMultis(List nUniques, List multis) {
    // Output.println("Starting multis: " + multis);
    // Output.println("Final uniques: " + nUniques);
    List nMultis = new ArrayList();
    for (Iterator li = multis.iterator(); li.hasNext();) {
      Index ix = (Index) li.next();
      boolean skip = false;
      for (ListIterator oli = nUniques.listIterator(); !skip && oli.hasNext();) {
        Index oix = (Index) oli.next();
        Index nix = oix.makeSuper(ix);
        if (nix != null) {
          // Output.println("U:Replacing " + oix.name + " with " + nix);
          oli.set(nix);
          skip = true;
        }
      }
      for (ListIterator oli = nMultis.listIterator(); !skip && oli.hasNext();) {
        Index oix = (Index) oli.next();
        Index nix = oix.makeSuper(ix);
        if (nix != null) {
          if (oix.size() >= ix.size()) {
            // Output.println("M:Replacing " + oix.name + " with " + nix);
            oli.set(nix);
            skip = true;
          } else {
            // Output.println("M:Removing " + oix.name);
            oli.remove();
            ix = nix;
          }
        }
      }
      if (!skip) nMultis.add(ix);
    }
    // Output.println("Final final uniques: " + nUniques);
    // Output.println("Final multis: " + nMultis);
    return nMultis;
  }

  public static void main(String[] args) {
    List uniques = new ArrayList();
    List multis = new ArrayList();
    List indexes = uniques;
    Index ix = new Index();
    String basename = "unique_";
    for (int i = 0; i < args.length; i++) {
      if (",".equals(args[i])) {
        ix.cut();
      } else if (".".equals(args[i])) {
        if (ix.isEmpty()) {
          if (indexes != uniques) {
            throw new RuntimeException("Can only specify . . once");
          }
          indexes = multis;
          basename = "multi_";
        } else {
          indexes.add(ix);
          ix.name = basename + i;
          ix.isUnique = (indexes == uniques);
          ix = new Index();
        }
      } else {
        ix.add(args[i]);
      }
    }
    ix.name = basename + args.length;
    indexes.add(ix);
    Output.println("Starting uniques: " + uniques);
    Output.println("Starting multis:  " + multis);

    List nUniques = trimUniques(uniques);
    List nMultis = trimMultis(nUniques, multis);

    Output.println("Final uniques: " + nUniques);
    Output.println("Final multis: " + nMultis);
  }
}
