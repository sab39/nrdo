///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.cgl.Eval
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2001/08/28
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
import java.io.File;
import java.io.IOException;
import java.io.Writer;
import java.util.ArrayList;
import java.util.Iterator;
import java.util.List;

import net.netreach.util.FileLocatedException;

class Eval extends Expr implements Dependency {
  Expr filename;
  File relativeTo;
  List activeDeps;
  long modified;

  // We need to store two different pieces of information in the context's
  // "deps" map. In order to do this we use "this" as one key and "key" as the
  // other.
  private Object key = new Object();

  public TimestampedObject resolve(Context ctx) throws IOException,
      FileLocatedException {
    TimestampedObject tso = (TimestampedObject) ctx.deps.get(this);
    if (tso == null) {
      String fname = filename.evaluate(ctx).toString();
      tso = (TimestampedObject) CGLParser.loadTemplate(new File(relativeTo,
          fname));
      ctx.deps.put(this, tso);
    }
    long modTime = modified;
    for (Iterator i = activeDeps.iterator(); i.hasNext();) {
      Dependency dep = (Dependency) i.next();
      long depMod = dep.resolve(ctx).resolve(ctx);
      if (depMod > modTime) modTime = depMod;
    }
    ctx.deps.put(key, new Long(modTime));
    return tso;
  }

  public Object evaluate(Context ctx) throws FileLocatedException, IOException {
    Expr exp = getFile(ctx);
    long oldMod = ctx.callerModified;
    ctx.callerModified = ((Long) ctx.deps.get(key)).longValue();
    if (oldMod > ctx.callerModified) ctx.callerModified = oldMod;
    Object res = exp.evaluate(ctx);
    ctx.callerModified = oldMod;
    return res;
  }

  public void write(Context ctx, Writer out) throws FileLocatedException, IOException {
    Expr exp = getFile(ctx);
    long oldMod = ctx.callerModified;
    ctx.callerModified = ((Long) ctx.deps.get(key)).longValue();
    if (oldMod > ctx.callerModified) ctx.callerModified = oldMod;
    exp.write(ctx, out);
    ctx.callerModified = oldMod;
  }

  Eval(File relativeTo, List activeDeps, long modified, Expr filename) {
    this.relativeTo = relativeTo;
    this.filename = filename;
    this.modified = modified;
    this.activeDeps = new ArrayList(activeDeps);
  }

  Expr getFile(Context ctx) throws IOException, FileLocatedException {
    return (Expr) resolve(ctx);
  }
}
