///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.cgl.SepLDefn
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2002/06/20
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
import java.util.Iterator;
import java.util.LinkedList;
import java.util.List;

import net.netreach.util.FileLocatedException;

public class SepLDefn extends LDefnElem {
  LDefnElem sep;
  List body;

  SepLDefn(LDefnElem sep, List body) {
    this.sep = sep;
    this.body = body;
  }

  public void addTo(List target, Context ctx, List sep) throws FileLocatedException,
      IOException {
    Object dummy = new Object();
    List sepResult = new LinkedList();
    this.sep.addTo(sepResult, ctx);
    sepResult.add(dummy); // This lets us reliably test result.isEmpty() to
    // determine whether anything happened, even if
    // sepResult is otherwise empty. "dummy" gets filtered
    // out later.
    List result = new LinkedList();
    for (Iterator i = body.iterator(); i.hasNext();) {
      LDefnElem item = (LDefnElem) i.next();
      item.addTo(result, ctx, sepResult);
    }
    if (sep != null && !result.isEmpty()) target.addAll(sep);
    for (Iterator i = result.iterator(); i.hasNext();) {
      if (i.next() == dummy) i.remove();
    }
    if (!result.isEmpty()) {
      target.addAll(result.subList(sepResult.size() - 1, result.size()));
    }
  }
}
