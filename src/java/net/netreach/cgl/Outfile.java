///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.cgl.Outfile
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
import java.io.BufferedWriter;
import java.io.File;
import java.io.FileWriter;
import java.io.IOException;
import java.io.StringWriter;
import java.io.Writer;
import java.util.HashSet;
import java.util.Iterator;
import java.util.List;

import net.netreach.util.CVSFile;
import net.netreach.util.FileLocatedException;
import net.netreach.util.Output;

class Outfile extends Expr {
  Expr filename;
  Expr body;
  long timestamp;
  List innerDeps;
  List outerDeps;
  boolean paranoid;

  Outfile(long timestamp, List innerDeps, List outerDeps, Expr filename,
      Expr body) {
    this.timestamp = timestamp;
    this.innerDeps = innerDeps;
    this.outerDeps = outerDeps;
    this.filename = filename;
    this.body = body;
  }

  long lastModified(Context ctx) throws FileLocatedException, IOException {
    Long val = (Long) ctx.deps.get(this);
    if (val != null) return val.longValue();
    long stamp = timestamp;
    for (Iterator i = innerDeps.iterator(); i.hasNext();) {
      long iStamp = ((Dependency) i.next()).resolve(ctx).resolve(ctx);
      if (iStamp > stamp) stamp = iStamp;
    }
    for (Iterator i = outerDeps.iterator(); i.hasNext();) {
      long iStamp = ((Dependency) i.next()).resolve(ctx).resolve(ctx);
      if (iStamp > stamp) stamp = iStamp;
    }
    ctx.deps.put(this, new Long(stamp));
    return stamp;
  }

  private static HashSet paranoidWritten = new HashSet();
  public static void reset() {
    paranoidWritten.clear();
  }

  public Object evaluate(Context ctx) throws FileLocatedException, IOException {
    if (ctx.srcbase == null) {
      throw new IOException("This template not permitted to output");
    }
    String file = filename.evaluate(ctx).toString();
    File f = new File(ctx.srcbase, file);
    if (ctx.age == 0 || !f.exists() || lastModified(ctx) > f.lastModified()
        || ctx.age > f.lastModified() || ctx.callerModified > f.lastModified()) {
      if (paranoid && paranoidWritten.contains(f)) return null;
      Output.println((paranoid ? "Constructing " : "Writing ") + file + ".");
      f.getParentFile().mkdirs();
      try {
        Writer out = paranoid ? (Writer) new StringWriter()
            : new BufferedWriter(new FileWriter(f));
        try {
          body.write(ctx, out);
        } finally {
          out.close();
        }
        if (paranoid) {
          CVSFile cf = new CVSFile(f);
          String contents = out.toString();
          if (!cf.exists() || !cf.getContents().equals(contents)) {
            Output.println("Writing " + file + ".");
            cf.setContents(contents);
          } else {
            paranoidWritten.add(f);
          }
        }
      } catch (IOException e) {
        f.delete();
        throw e;
      } catch (FileLocatedException e) {
        f.delete();
        throw e;
      } catch (InterruptedException e) {
        f.delete();
        throw new IOException(e.toString());
      } catch (RuntimeException e) {
        f.delete();
        throw e;
      }
    }
    return null;
  }
}
