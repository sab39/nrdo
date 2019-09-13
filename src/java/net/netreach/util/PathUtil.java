///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.util.misc.PathUtil
//
//      Author: Stuart Ballard <sballard@netreach.com>
//  Created On: 2003/11/18
// Description: Path-related utilities
///////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////////////
// nrdo - Object-Relational database development tool (runtime libraries).
// Copyright (c) 2000-2003 NetReach, Inc.
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

package net.netreach.util;

// I/O classes used.
import java.io.File;
import java.io.IOException;

public class PathUtil {

  // In Classpath and IKVM, File.getCanonicalPath/File are not implemented
  // correctly for UNC paths. These methods bypass the internal implementation
  // and recognize that paths starting with \\ are canonical already.
  public static File canonicalFile(File file) throws IOException {
    if (file.toString().startsWith("\\\\")) {
      return file;
    } else {
      return file.getCanonicalFile();
    }
  }

  public static String canonicalPath(File file) throws IOException {
    if (file.toString().startsWith("\\\\")) {
      return file.getPath();
    } else {
      return file.getCanonicalPath();
    }
  }
}