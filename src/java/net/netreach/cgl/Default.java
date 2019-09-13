///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.cgl.Default
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2002/06/17
// Description: Part of CGL interpreter
///////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////////////
// nrdo - Object-Relational database development tool (runtime libraries).
// Copyright (c) 2000-2002 NetReach, Inc.
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
import java.io.Writer;

import net.netreach.util.FileLocatedException;

class Default extends Expr {
  String name;
  Expr def;

  public Object evaluate(Context ctx) throws FileLocatedException, IOException {
    Object o = ctx.vars.get(name);
    if (o == null && !ctx.vars.containsKey(name)) {
      return def.evaluate(ctx);
    } else {
      return o;
    }
  }

  void write(Context ctx, Writer out) throws FileLocatedException, IOException {
    if (!ctx.vars.containsKey(name)) {
      def.write(ctx, out);
    } else {
      super.write(ctx, out);
    }
  }

  Default(String name, Expr def) {
    this.name = name;
    this.def = def;
  }
}
