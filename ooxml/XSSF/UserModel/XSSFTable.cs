/* ====================================================================
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for Additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
==================================================================== */

namespace NPOI.XSSF.usermodel;

using java.io.IOException;
using java.io.InputStream;
using java.io.OutputStream;
using java.util.Arrays;
using java.util.List;
using java.util.Vector;

using org.apache.poi.POIXMLDocumentPart;
using org.apache.poi.Openxml4j.opc.PackagePart;
using org.apache.poi.Openxml4j.opc.PackageRelationship;
using NPOI.SS.util.CellReference;
using NPOI.XSSF.usermodel.helpers.XSSFXmlColumnPr;
using org.apache.xmlbeans.XmlException;
using org.Openxmlformats.schemas.spreadsheetml.x2006.main.CTTable;
using org.Openxmlformats.schemas.spreadsheetml.x2006.main.CTTableColumn;
using org.Openxmlformats.schemas.spreadsheetml.x2006.main.TableDocument;

/**
 * 
 * This class : the Table Part (Open Office XML Part 4:
 * chapter 3.5.1)
 * 
 * This implementation works under the assumption that a table Contains mappings to a subtree of an XML.
 * The root element of this subtree an occur multiple times (one for each row of the table). The child nodes
 * of the root element can be only attributes or element with maxOccurs=1 property set
 * 
 *
 * @author Roberto Manicardi
 */
public class XSSFTable : POIXMLDocumentPart {
	
	private CTTable ctTable;
	private List<XSSFXmlColumnPr> xmlColumnPr;
	private CellReference startCellReference;
	private CellReference endCellReference;	
	private String commonXPath; 
	
	
	public XSSFTable() {
		base();
		ctTable = CTTable.Factory.newInstance();

	}

	public XSSFTable(PackagePart part, PackageRelationship rel)
		  {
		base(part, rel);
		ReadFrom(part.GetInputStream());
	}

	public void ReadFrom(InputStream is)  {
		try {
			TableDocument doc = TableDocument.Factory.Parse(is);
			ctTable = doc.GetTable();
		} catch (XmlException e) {
			throw new IOException(e.GetLocalizedMessage());
		}
	}
	
	public XSSFSheet GetXSSFSheet(){
		return (XSSFSheet) GetParent();
	}

	public void WriteTo(OutputStream out)  {
		TableDocument doc = TableDocument.Factory.newInstance();
		doc.SetTable(ctTable);
		doc.save(out, DEFAULT_XML_OPTIONS);
	}

	
	protected void Commit()  {
		PackagePart part = GetPackagePart();
		OutputStream out = part.GetOutputStream();
		WriteTo(out);
		out.Close();
	}
	
	public CTTable GetCTTable(){
		return ctTable;
	}
	
	/**
	 * Checks if this Table element Contains even a single mapping to the map identified by id
	 * @param id the XSSFMap ID
	 * @return true if the Table element contain mappings
	 */
	public bool mapsTo(long id){
		bool maps =false;
		
		List<XSSFXmlColumnPr> pointers = GetXmlColumnPrs();
		
		for(XSSFXmlColumnPr pointer: pointers){
			if(pointer.GetMapId()==id){
				maps=true;
				break;
			}
		}
		
		return maps;
	}

	
	/**
	 * 
	 * Calculates the xpath of the root element for the table. This will be the common part
	 * of all the mapping's xpaths
	 * 
	 * @return the xpath of the table's root element
	 */
	public String GetCommonXpath() {
		
		if(commonXPath == null){
		
		String[] commonTokens ={};
		
		for(CTTableColumn column :ctTable.GetTableColumns().getTableColumnList()){
			if(column.GetXmlColumnPr()!=null){
				String xpath = column.GetXmlColumnPr().getXpath();
				String[] tokens =  xpath.split("/");
				if(commonTokens.Length==0){
					commonTokens = tokens;
					
				}else{
					int maxLenght = commonTokens.Length>tokens.Length? tokens.Length:commonTokens.Length;
					for(int i =0; i<maxLenght;i++){
						if(!commonTokens[i].Equals(tokens[i])){
						 List<String> subCommonTokens = Arrays.asList(commonTokens).GetRange(0, i);
						 
						 String[] Container = {};
						 
						 commonTokens = subCommonTokens.ToArray(Container);
						 break;
						 
						 
						}
					}
				}
				
			}
		}
		
		
		commonXPath ="";
		
		for(int i = 1 ; i< commonTokens.Length;i++){
			commonXPath +="/"+commonTokens[i];
		
		}
		}
		
		return commonXPath;
	}

	
	public List<XSSFXmlColumnPr> GetXmlColumnPrs() {
		
		if(xmlColumnPr==null){
			xmlColumnPr = new Vector<XSSFXmlColumnPr>();
			for(CTTableColumn column:ctTable.GetTableColumns().getTableColumnList()){
				if(column.GetXmlColumnPr()!=null){
					XSSFXmlColumnPr columnPr = new XSSFXmlColumnPr(this,column,column.GetXmlColumnPr());
					xmlColumnPr.Add(columnPr);
				}
			}
		}
		return xmlColumnPr;
	}
	
	/**
	 * @return the name of the Table, if set
	 */
	public String GetName() {
	   return ctTable.GetName();
	}
	
	/**
	 * Changes the name of the Table
	 */
	public void SetName(String name) {
	   if(name == null) {
	      ctTable.unSetName();
	      return;
	   }
	   ctTable.SetName(name);
	}

   /**
    * @return the display name of the Table, if set
    */
   public String GetDisplayName() {
      return ctTable.GetDisplayName();
   }

   /**
    * Changes the display name of the Table
    */
   public void SetDisplayName(String name) {
      ctTable.SetDisplayName(name);
   }

	/**
	 * @return  the number of mapped table columns (see Open Office XML Part 4: chapter 3.5.1.4)
	 */
	public long GetNumerOfMappedColumns(){
		return ctTable.GetTableColumns().getCount();
	}
	
	
	/**
	 * @return The reference for the cell in the top-left part of the table
	 * (see Open Office XML Part 4: chapter 3.5.1.2, attribute ref) 
	 *
	 */
	public CellReference GetStartCellReference() {
		
		if(startCellReference==null){			
				String ref = ctTable.GetRef();
				String[] boundaries = ref.split(":");
				String from = boundaries[0];
				startCellReference = new CellReference(from);
		}
		return startCellReference;
	}
	
	/**
	 * @return The reference for the cell in the bottom-right part of the table
	 * (see Open Office XML Part 4: chapter 3.5.1.2, attribute ref)
	 *
	 */
	public CellReference GetEndCellReference() {
		
		if(endCellReference==null){
			
				String ref = ctTable.GetRef();
				String[] boundaries = ref.split(":");
				String from = boundaries[1];
				endCellReference = new CellReference(from);
		}
		return endCellReference;
	}
	
	
	/**
	 *  @return the total number of rows in the selection. (Note: in this version autofiltering is ignored)
	 *
	 */
	public int GetRowCount(){
		
		
		CellReference from = GetStartCellReference();
		CellReference to = GetEndCellReference();
		
		int rowCount = -1;
		if (from!=null && to!=null){
		 rowCount = to.GetRow()-from.getRow();
		}
		return rowCount;
	}
}
