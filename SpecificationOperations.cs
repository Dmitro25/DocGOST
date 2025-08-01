﻿/*
 *
 * This file is part of the DocGOST project.    
 * Copyright (C) 2018 Vitalii Nechaev.
 * 
 * This program is free software; you can redistribute it and/or modify it 
 * under the terms of the GNU Affero General Public License version 3 as 
 * published by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using DocGOST.Data;
using iTextSharp.text.pdf.events;
using static iTextSharp.text.pdf.AcroFields;


namespace DocGOST
{
    static class SpecificationOperations
    {
        const int maxNameLength = 34;// _DVK
        const int maxNoteLength = 13;
        private enum CombineType { ctUnknone, ctRegular, ctHie };

        static public long MakeDesignator_Special(string designator) {
            var des = Global.GetDesignatorValue(designator);
            if (Global.ExtractDesignatorHieBlockNum(des) == 0) return des;
            return Global.SwapDesignatorGroupAndSelfNum(des);
        }

        // Если bConstPosMode=true, то используется такой режим спецификации, в котором в колонке "Поз" уже проставлены и перенумеровывать и менять порядок строк не нужно
        static public List<SpecificationItem> groupSpecificationElements(List<SpecificationItem> sList, bool bConstPosMode, ref int numberOfValidStrings)
        {
            #region Группировка элементов спецификации из раздела "Прочие" с одинаковым наименованием, которые идут подряд            
            List<SpecificationItem> tempList = new List<SpecificationItem>();
            SpecificationItem tempItem = new SpecificationItem();

            string prevElemName = sList[0].name;
            int position = 0;


            tempItem.makeEmpty();
            tempItem.spSection = sList[0].spSection;
            tempItem.position = (position + 1).ToString();
            tempItem.name = sList[0].name;
            tempItem.quantity = "1";
            tempItem.note = sList[0].designator;
            tempItem.docum = sList[0].docum;
            tempItem.group = sList[0].group;

            long prevDesignatorValue = MakeDesignator_Special(sList[0].designator);

            for (int i = 1; i < numberOfValidStrings; i++)
            {
                long designatorValue = MakeDesignator_Special(sList[i].designator);

                if ( (sList[i].name == prevElemName) && (designatorValue == prevDesignatorValue + 1) )
                {
                    tempItem.note += ", " + sList[i].note;
                    tempItem.quantity = (int.Parse(tempItem.quantity) + 1).ToString();
                }
                else
                {
                    tempList.Add(tempItem);
                    position++;
                    tempItem = new SpecificationItem();
                    tempItem.makeEmpty();
                    tempItem.spSection = sList[i].spSection;
                    tempItem.position = (position + 1).ToString();
                    tempItem.name = sList[i].name;
                    tempItem.quantity = "1";
                    tempItem.note = sList[i].designator;
                    tempItem.docum = sList[i].docum;
                    tempItem.group = sList[i].group;
                }
                if (i == (numberOfValidStrings - 1)) { tempList.Add(tempItem); position++; }
                prevElemName = sList[i].name;
                prevDesignatorValue = designatorValue;
            }

            numberOfValidStrings = position;

            for (int i = 0; i < numberOfValidStrings; i++)
            {
                var designators = tempList[i].note.Split(new string[] {", "}, StringSplitOptions.None);
                if (designators.Length > 2) tempList[i].note = designators[0] + "..." + designators[designators.Length - 1];
            }


            #endregion

            #region Группировка всех элементов спецификации из раздела "Прочие" с одинаковым наименованием
            {
                var sortList = tempList.OrderBy(x => x.name).ToList();
                int orig = 0;
                for (var i = 1; i < numberOfValidStrings; i++) {
                    if ((sortList[orig].name == sortList[i].name) && (sortList[orig].name != String.Empty)) {
                        sortList[orig].note += ", " + sortList[i].note;
                        sortList[orig].quantity = (int.Parse(sortList[orig].quantity) + int.Parse(sortList[i].quantity)).ToString();
                        sortList[i].name = string.Empty;
                    } else
                        orig = i;
                }
                //for (int i = 0; i < numberOfValidStrings; i++) // _DVK выполняется слишком долго при большом числе элементов
                //{
                //    for (int j = i + 1; j < numberOfValidStrings; j++)
                //        if ((tempList[j].name == tempList[i].name) && (tempList[j].name != String.Empty))
                //        {
                //            tempList[i].note += ", " + tempList[j].note;
                //            tempList[i].quantity = (int.Parse(tempList[i].quantity) + int.Parse(tempList[j].quantity)).ToString();
                //            tempList[j].name = String.Empty;
                //        }
                //}
            }
            #endregion

            #region Удаление лишних строк и сортировка по алфавиту
            List<SpecificationItem> tempList1 = new List<SpecificationItem>();

            for (int i = 0; i < numberOfValidStrings; i++)
            {
                if (tempList[i].name != String.Empty)
                {
                    tempItem = new SpecificationItem();
                    tempItem.makeEmpty();
                    tempItem.spSection = tempList[i].spSection;
                    tempItem.name = tempList[i].name;
                    tempItem.quantity = tempList[i].quantity;
                    tempItem.note = tempList[i].note;
                    tempItem.docum = tempList[i].docum;
                    tempItem.group = tempList[i].group;
                    tempList1.Add(tempItem);
                }
            }

            tempList = new List<SpecificationItem>();
            tempList = tempList1.OrderBy(x => x.name).ToList();

            foreach (SpecificationItem item in tempList) item.position = SpecificationItem.POS_AUTO;

            numberOfValidStrings = tempList.Count;
            #endregion

            
            #region Разбиение каждой записи на нужное количество строк
            List<SpecificationItem> tempList2 = new List<SpecificationItem>();
            //Удаление лишних строк
            for (int i = 0; i < numberOfValidStrings; i++)
            {
                if (tempList[i].name != String.Empty)
                {
                    //Разбиение строк, чтобы все надписи вмещались в ячейки.

                    string name = /*tempList[i].group + " " +*/ tempList[i].name/* + (tempList[i].docum != "" ? " " + tempList[i].docum : "")*/;
                    string note = tempList[i].note;
                    string quantity = tempList[i].quantity;
                    string pos = tempList[i].position;

                    //string group = tempList[i].group; // _DVK
                    //string docum = tempList[i].docum;

                    while ((name != String.Empty) | (note != String.Empty))
                    {
                        tempItem = new SpecificationItem();
                        tempItem.makeEmpty();
                        tempItem.spSection = tempList[i].spSection;
                        if (pos != String.Empty)
                        {
                            tempItem.position = tempList[i].position;
                            pos = String.Empty;
                        }                        
                        if (quantity != String.Empty)
                        {
                            tempItem.quantity = quantity;
                            quantity = String.Empty;
                        }

                        tempItem.name = Global.ParseItersTillLen(ref name, maxNameLength, " ", true);
                        //Разбираемся с наименованием
                        //if (name.Length > maxNameLength)
                        //{
                        //    if ((name.Length - docum.Length - 1) < maxNameLength)
                        //    {
                        //        tempItem.name = name.Substring(0, name.Length - docum.Length - 1);
                        //        name = docum;
                        //        docum = String.Empty;
                        //    }
                        //    else
                        //    if (group != String.Empty)
                        //    {
                        //        tempItem.name = group;
                        //        name = name.Replace(group + ' ', String.Empty);
                        //        group = String.Empty;
                        //    }
                        //    else
                        //    {
                        //        tempItem.name = Global.ParseItersTillLen(ref name, maxNameLength, ' ');
                        //    }
                        //}
                        //else if (name != String.Empty)
                        //{
                        //    tempItem.name = name;
                        //    name = String.Empty;
                        //}

                        //Разбираемся с примечанием
                        tempItem.note = Global.ParseItersTillLen(ref note, maxNoteLength, ", ", false);
                        //if (note.Length > maxNoteLength)
                        //{
                        //    string[] designators = note.Split(new Char[] { ',' });
                        //    tempItem.note = designators[0] + ", ";

                        //    for (int j = 1; j < designators.Length; j++)
                        //    {
                        //        if ((tempItem.note.Length + designators[j].Length) >= maxNoteLength)
                        //        {
                        //            note = string.Empty;
                        //            for (int k = j; k < designators.Length; k++)
                        //                if (k != (designators.Length - 1)) note += designators[k] + ", ";
                        //                else note += designators[k];
                        //            break;
                        //        }
                        //        else tempItem.note += designators[j] + ", ";
                        //    }
                        //}
                        //else if (note != String.Empty)
                        //{
                        //    tempItem.note = note;
                        //    note = String.Empty;
                        //}


                        tempList2.Add(tempItem);
                    }


                }

            }

            numberOfValidStrings = tempList2.Count();

            #endregion

            return tempList2;
        }

        static public List<SpecificationItem> BreakLongLinesOnly(List<SpecificationItem> sList) {
            var rslt = new List<SpecificationItem>();
            foreach (var item in sList) {
                if (item.name.Length <= maxNameLength) {
                    rslt.Add(item);
                }
                else {
                    string name = item.name;
                    item.name = Global.ParseItersTillLen(ref name, maxNameLength, " ", true);
                    rslt.Add(item);
                    while (name != "") {
                        var item2 = new SpecificationItem();
                        item2.makeEmpty();
                        item2.spSection = item.spSection;
                        item2.group = item.group;
                        item2.docum = item.docum;
                        item2.name = Global.ParseItersTillLen(ref name, maxNameLength, " ", true);
                        rslt.Add(item2);
                    }
                }
            }
            for (int i = 0; i < rslt.Count; i++)
                rslt[i].id = i + 1;
            return rslt;
        }

    }
}
