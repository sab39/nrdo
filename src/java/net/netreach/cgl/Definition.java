///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.cgl.Definition
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2001/07/24
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

// I/O classes.
import java.io.IOException;
import java.util.Iterator;
import java.util.LinkedList;
import java.util.List;
import java.util.Map;

import net.netreach.util.FileLocatedException;

public class Definition {

  List elems = new LinkedList();

  public Context setOnContext(Context ctx) throws FileLocatedException, IOException {
    for (Iterator i = elems.iterator(); i.hasNext();) {
      DefnElem elem = (DefnElem) i.next();
      ctx = elem.setOnContext(ctx);
    }
    return ctx;
  }

  public void setOnMap(Map map) throws FileLocatedException, IOException {
    setOnMapInternal(map, map);
  }

  public void setOnMap(Map map, Map backing) throws FileLocatedException, IOException {
    setOnMapInternal(new Scope(backing, map), map);
  }

  private void setOnMapInternal(Map map, Map putOn) throws FileLocatedException,
      IOException {
    Context ctx = new Context();
    ctx.vars = new Scope(map);
    ctx.vars.setNoCache(true);
    if (setOnContext(ctx) != ctx) {
      throw new RuntimeException("Illegal definition in set on map");
    }
    putOn.putAll(ctx.vars.getFront());
  }

  public Context createContext(Context ctx) throws FileLocatedException, IOException {
    Context ictx = setOnContext(new Context(ctx));
    ictx.vars.freeze();
    return ictx;
  }
}
