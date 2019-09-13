///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.cgl.Use
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
import java.util.Iterator;

import net.netreach.util.FileLocatedException;

class Use extends DefnElem {
  String name;

  Use(String name) {
    this.name = name;
  }

  public Context setOnContext(Context ctx) throws FileLocatedException, IOException {
    Declosure closure = (Declosure) ctx.vars.get("declare$" + name);
    if (closure == null) {
      throw new RuntimeException("No such declare: " + name /*
                                                             * +
                                                             * "\nExisting vars: "
                                                             * +
                                                             * ctx.vars.keySet()
                                                             */);
    }
    Declare decl = closure.decl;
    Context ictx = new Context(closure.ctx);
    for (Iterator i = decl.given.iterator(); i.hasNext();) {
      Object o = i.next();
      ictx.vars.put(o, ctx.vars.get(o));
    }
    ictx = new Context(ictx);
    ictx.vars.setNoCache(true);
    Context res = decl.decl.setOnContext(ictx);
    if (res != ictx) {
      throw new RuntimeException("Illegal operation in 'use'");
    }
    ctx.vars.putAll(ictx.vars.getFront());
    return ctx;
  }
}
