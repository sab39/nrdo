///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.smelt.SmeltException
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2001/08/17
// Description: An exception that occurred related to a Smelt file.
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

package net.netreach.smelt;

import java.io.File;

import net.netreach.util.FileLocatedException;
import net.netreach.util.FileLocation;

public class SmeltException extends FileLocatedException {
  private static final long serialVersionUID = -5332008960959649104L;

  protected SmeltException(File file, String msg, int startRow,
      int startCol, int endRow, int endCol) {
    this(new FileLocation(file, startRow, startCol, endRow, endCol), msg);
  }

  public SmeltException(FileLocation loc, String msg) {
    super(loc, msg);
  }
}
