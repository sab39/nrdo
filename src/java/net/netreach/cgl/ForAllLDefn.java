///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.cgl.ForAllLDefn
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2001/07/31
// Description: Part of cgl interpreter.
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

// Collections classes.
import java.io.IOException;
import java.util.Collection;
import java.util.Iterator;
import java.util.List;
import java.util.Map;

import net.netreach.util.FileLocatedException;

public class ForAllLDefn extends LDefnElem {
  Expr list;
  LDefnElem body;

  ForAllLDefn(Expr list, LDefnElem body) {
    this.list = list;
    this.body = body;
  }

  public void addTo(List target, Context ctx, List sep) throws FileLocatedException,
      IOException {
    Collection lres = (Collection) list.evaluate(ctx);
    for (Iterator i = lres.iterator(); i.hasNext();) {
      Context ictx = new Context(ctx);
      ictx.vars.setFront((Map) i.next());
      ictx.vars.setNoCache(true);
      ictx = new Context(ictx);
      body.addTo(target, ictx, sep);
    }
  }
}
