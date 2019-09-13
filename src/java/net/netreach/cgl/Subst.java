///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.cgl.Subst
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2001/05/24
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

import net.netreach.util.FileLocatedException;

class Subst extends Expr {
  Expr mapFrom;
  Expr mapTo;
  Expr value;

  public Object evaluate(Context ctx) throws FileLocatedException, IOException {
    Object result = value.evaluate(ctx);
    if (result == null) throw new NullPointerException("Illegal use of null");
    Object fromResult = mapFrom.evaluate(ctx);
    if (fromResult == null)
      throw new NullPointerException("Illegal use of null");
    String from = fromResult.toString();
    if (from.length() == 0)
      throw new IllegalArgumentException("From cannot be empty in subst");
    Object toResult = mapTo.evaluate(ctx);
    String to = (toResult == null ? "" : toResult.toString());
    String s = result.toString();
    StringBuffer sb = new StringBuffer(s.length());
    int pos = 0;
    while (pos < s.length()) {
      int newPos = s.indexOf(from, pos);
      if (newPos >= 0) {
        sb.append(s.substring(pos, newPos));
        sb.append(to);
        pos = newPos + from.length();
      } else {
        sb.append(s.substring(pos));
        pos = s.length();
      }
    }
    return sb.toString();
  }

  Subst(Expr mapFrom, Expr mapTo, Expr value) {
    this.mapFrom = mapFrom;
    this.mapTo = mapTo;
    this.value = value;
  }
}
