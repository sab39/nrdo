///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.cgl.InterCap
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2001/05/21
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
import net.netreach.util.FileLocation;

class InterCap extends PositionedExpr {
  String chars;
  Expr value;

  public Object evaluate(Context ctx) throws FileLocatedException, IOException {
    Object result = value.evaluate(ctx);
    if (result == null) throw new CGLException(this, "Illegal use of null");
    String s = result.toString();
    StringBuffer sb = new StringBuffer(s.length());
    for (int i = 0; i < s.length(); i++) {
      char ch = s.charAt(i);
      if (chars.indexOf(ch) >= 0 && ++i < s.length()) {
        sb.append(Character.toUpperCase(s.charAt(i)));
      } else {
        sb.append(ch);
      }
    }
    return sb.toString();
  }

  InterCap(FileLocation loc, String chars, Expr value) {
    super(loc);
    this.chars = chars;
    this.value = value;
  }
}
