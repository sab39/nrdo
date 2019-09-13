///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.cgl.ForAll
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2001/05/30
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
import java.io.Writer;
import java.util.Collection;
import java.util.Iterator;
import java.util.Map;

import net.netreach.util.FileLocatedException;

class ForAll extends Expr {
  Expr list;
  Expr value;

  ForAll(Expr list, Expr value) {
    this.list = list;
    this.value = value;
  }

  public Object evaluate(Context ctx) throws FileLocatedException, IOException {
    return evaluate(ctx, null);
  }

  public Object evaluate(Context ctx, String sepr) throws FileLocatedException,
      IOException {
    Object result = null;
    StringBuffer sb = null;
    Collection exprs = (Collection) list.evaluate(ctx);
    for (Iterator i = exprs.iterator(); i.hasNext();) {
      Context ictx = new Context(ctx);
      ictx.vars.setFront((Map) i.next());
      ictx.vars.freeze();
      ictx.vars.setNoCache(true);
      ictx = new Context(ictx);
      Object exprResult = value.evaluate(ictx);
      if (exprResult != null) {
        if (result == null) {
          result = exprResult;
        } else {
          if (sb == null) {
            sb = new StringBuffer();
            sb.append(result);
          }
          if (sepr != null) sb.append(sepr);
          sb.append(exprResult);
        }
      }
    }
    return sb != null ? sb.toString() : result;
  }

  void write(Context ctx, Writer out) throws FileLocatedException, IOException {
    write(ctx, out, false);
  }

  void write(Context ctx, Writer out, boolean sep) throws FileLocatedException,
      IOException {
    SepWriter sout = sep ? (SepWriter) out : null;
    Collection exprs = (Collection) list.evaluate(ctx);
    for (Iterator i = exprs.iterator(); i.hasNext();) {
      Context ictx = new Context(ctx);
      ictx.vars.setFront((Map) i.next());
      ictx.vars.freeze();
      value.write(ictx, out);
      if (sep) sout.nextSep();
    }
  }
}
