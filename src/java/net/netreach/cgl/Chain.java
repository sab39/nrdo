///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.cgl.Chain
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
import java.io.Writer;
import java.util.Iterator;
import java.util.List;

import net.netreach.util.FileLocatedException;

class Chain extends Expr {
  List exprs;
  Expr sep;

  Chain(List exprs) {
    this(exprs, null);
  }

  Chain(List exprs, Expr sep) {
    this.exprs = exprs;
    this.sep = sep;
  }

  public Object evaluate(Context ctx) throws FileLocatedException, IOException {
    Object result = null;
    String sepr = null;
    if (sep != null) {
      Object sepResult = sep.evaluate(ctx);
      if (sepResult != null) sepr = sepResult.toString();
    }
    StringBuffer sb = null;
    for (Iterator i = exprs.iterator(); i.hasNext();) {
      Expr e = (Expr) i.next();
      Object exprResult;
      if (sepr != null && e instanceof ForAll) {
        exprResult = ((ForAll) e).evaluate(ctx, sepr);
      } else {
        exprResult = e.evaluate(ctx);
      }
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
    SepWriter sout = null;
    if (sep != null) {
      Object sepResult = sep.evaluate(ctx);
      if (sepResult != null) {
        if (out instanceof SepWriter) {
          sout = (SepWriter) out;
        } else {
          sout = new SepWriter(out);
        }
        out = sout;
        sout.startSep(sepResult.toString());
      }
    }
    for (Iterator i = exprs.iterator(); i.hasNext();) {
      Expr e = (Expr) i.next();
      if (sout != null && e instanceof ForAll) {
        ((ForAll) e).write(ctx, out, true);
      } else {
        e.write(ctx, out);
      }
      if (sout != null) sout.nextSep();
    }
    if (sout != null) sout.endSep();
  }
}
