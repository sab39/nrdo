///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.nrdo.tools.DependTable
//
//      Author: Stuart Ballard <sballard@netreach.com>
//  Created On: 2007/02/08
// Description: A table loaded from a dependent DfnBase
///////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////////////
// nrdo - Object-Relational database development tool.
// Copyright (c) 2000-2007 NetReach, Inc.
//
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// The GNU General Public License should be located in the file COPYING.gpl.
// For more information on the specific license terms for nrdo, please see the
// file COPYING.
//
// For more information about nrdo, please contact nrdo@netreach.net or
// write to Stuart Ballard at NetReach Inc, 1 eCommerce Plaza, 124 S Maple
// Street, Ambler, PA  19002  USA.
///////////////////////////////////////////////////////////////////////////////

package net.netreach.nrdo.tools;

// Collections classes used to implement the appropriate behavior.
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import net.netreach.util.Mappable;

/**
 * A table loaded from a dependent DfnBase.
 */
public class DependTable implements Mappable {
  public DependTable(TableDef table) {
    this.table = table;
  }

  private final TableDef table;

  public TableDef getTable() {
    return table;
  }

  private Map asMap;

  public Map toMap() {
    if (asMap == null) {
      asMap = new HashMap();
      asMap.put("depmodule", table.getModule());
      asMap.put("depdbobject", table.getName());
      asMap.put("dephash", table.hash);
      List modparts = new ArrayList();
      String[] parts = table.module.split(":");
      String cumul = "";
      for (int i = 0; i < parts.length; i++) {
        String part = parts[i];
        if (cumul.length() != 0) cumul += ':';
        cumul += part;
        Map map = new HashMap();
        map.put("part", part);
        map.put("cumul", cumul);
        modparts.add(map);
      }
      asMap.put("depmodparts", modparts);
      asMap.put("depbasemodule", table.dfnbase.cfg.module);
    }
    return asMap;
  }

  public String getFullName() {
    return table.fullName;
  }
}
