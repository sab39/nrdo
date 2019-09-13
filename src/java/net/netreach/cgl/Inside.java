///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.cgl.Inside
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
import java.io.IOException;
import java.util.ArrayList;
import java.util.Collection;
import java.util.Iterator;
import java.util.List;
import java.util.ListIterator;
import java.util.Map;

import net.netreach.util.FileLocatedException;

class Inside extends DefnElem {
  String name;
  List given;
  Definition defns;

  Inside(String name, List given, Definition defns) {
    this.name = name;
    this.given = given;
    this.defns = defns;
  }

  public Context setOnContext(Context ctx) throws FileLocatedException, IOException {
    Collection orig = (Collection) ctx.vars.get(name);
    if (orig == null)
      throw new RuntimeException("Inside called on null list " + name);
    List updated = new ArrayList(orig);
    for (ListIterator i = updated.listIterator(); i.hasNext();) {
      Map m = (Map) i.next();
      Context ictx = new Context(ctx);
      for (Iterator j = given.iterator(); j.hasNext();) {
        Object o = j.next();
        if (m.containsKey(o)) ictx.vars.put(o, m.get(o));
      }
      ictx = new Context(ictx);
      ictx.vars.setNoCache(true);
      Context res = defns.setOnContext(ictx);
      if (res != ictx) {
        throw new RuntimeException("Illegal operation in 'inside'");
      }
      i.set(new Scope(m, ictx.vars.getFront()));
    }
    ctx.vars.put(name, updated);
    return ctx;
  }
}
