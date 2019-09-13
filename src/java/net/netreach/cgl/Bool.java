///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.cgl.Bool
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2001/05/14
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
import java.util.Collection;

import net.netreach.util.FileLocatedException;

class Bool extends Expr {
  Expr value;

  public Object evaluate(Context ctx) throws FileLocatedException, IOException {
    Object result = value.evaluate(ctx);
    return new Boolean(truth(result));
  }

  Bool(Expr value) {
    this.value = value;
  }

  /**
   * Test whether an variable should be evaluated as "true" or "false" when
   * encountered in a "when" clause. Currently, all objects evaluate as true
   * except the following: null, "" (the empty string), new Boolean(false), any
   * Collection object that isEmpty(), and any Number object whose doubleValue()
   * method returns 0. In particular, it is worth noting that the String values
   * "0" and "false" both evaluate as TRUE.
   * 
   * @param predicate
   *          The name of the variable to test.
   * @param vars
   *          The variable set to look for the variable in.
   */
  public static boolean truth(Object o) {
    return o != null
        && !(o instanceof Boolean && !((Boolean) o).booleanValue())
        && !(o instanceof Number && ((Number) o).doubleValue() == 0)
        && !(o instanceof String && ((String) o).length() == 0)
        && !(o instanceof Collection && ((Collection) o).isEmpty());
  }
}
