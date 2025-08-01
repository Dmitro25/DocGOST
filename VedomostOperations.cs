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
using System.Diagnostics;
using System.Linq;
using DocGOST.Data;

namespace DocGOST
{
    class VedomostOperations
    {
        const int maxNoteLength = 12;

        public List<VedomostItem> groupVedomostElements(List<VedomostItem> tempList, ref int numberOfValidStrings, bool bAllowSingleLineGroups = true)
        {
            const int maxNameLength = 32; // _DVK was 36

            //List<string> DivideLongLine(string str) {
            //    var cs2f = new char[] { ' ', '-' };
            //    int lineStart = 0;
            //    int pos = 0;
            //    var rslt = new List<string>();

            //    while (true) {
            //        int idx = str.IndexOfAny(cs2f, pos);
            //        if (idx >= 0) {
            //            bool isSpace = (str[idx] == ' ');
            //            if (!isSpace) idx++;
            //            if (idx - lineStart <= maxNameLength 
            //                || PdfOperations.MeasureTextWidth(str.Substring(lineStart, idx - lineStart), PdfOperations.VEDOMOST_NORMAL_FONT_SZ) < 10) {
            //                pos = isSpace ? idx + 1 : idx;
            //            }

            //                pos = idx + 1;
            //            }
            //            lineStart = idx;
            //                || )
            //    }
            //    return rslt;
            //}


            #region Группировка всех элементов ведомости с одинаковым наименованием

            VedomostItem tempItem = new VedomostItem();

            {
                var sList = new List<VedomostItem>( tempList );
                sList.Sort((a, b) => {
                    var rslt = a.name.CompareTo( b.name );
                    if (rslt != 0) return rslt;
                    rslt = a.docum.CompareTo(b.docum);
                    return rslt;
                });
                int orig = 0;
                for ( var i = 1; i < numberOfValidStrings; i++ ) {
                    if ((sList[orig].name == sList[i].name) && (sList[orig].docum == sList[i].docum) && (sList[orig].name != String.Empty)) { 
                        sList[orig].quantityIzdelie = (int.Parse(sList[orig].quantityIzdelie) + int.Parse(sList[i].quantityIzdelie)).ToString();
                        sList[orig].quantityTotal = sList[orig].quantityIzdelie;
                        sList[i].name = string.Empty;
                    }
                    else 
                        orig = i;
                }
                //for (int i = 0; i < numberOfValidStrings; i++) // _DVK выполняется слишком долго при большом числе элементов
                //{
                //    for (int j = i + 1; j < numberOfValidStrings; j++)
                //        if ((tempList[j].name == tempList[i].name) && (tempList[j].docum == tempList[i].docum) && (tempList[j].name != String.Empty))
                //        {
                //            tempList[i].quantityIzdelie = (int.Parse(tempList[i].quantityIzdelie) + int.Parse(tempList[j].quantityIzdelie)).ToString();
                //            tempList[i].quantityTotal = tempList[i].quantityIzdelie;
                //            tempList[j].name = string.Empty;
                //            tempList[j].note = string.Empty;
                //            tempList[j].auxNote = string.Empty;
                //        }
                //}
            }
            #endregion

            #region Удаление лишних строк и сортировка по алфавиту на уровне групп
            List<VedomostItem> tempList1 = new List<VedomostItem>();

            for (int i = 0; i < numberOfValidStrings; i++)
            {
                if (tempList[i].name != String.Empty)
                {
                    tempItem = new VedomostItem();
                    tempItem.makeEmpty();
                    tempItem.name = tempList[i].name;
                    tempItem.docum = tempList[i].docum;
                    tempItem.quantityIzdelie = tempList[i].quantityIzdelie;
                    tempItem.quantityTotal = tempList[i].quantityTotal;
                    tempItem.group = tempList[i].group;
                    tempItem.groupPlural = tempList[i].groupPlural;
                    tempItem.note = tempList[i].note;
                    tempItem.auxNote = tempList[i].auxNote;
                    tempItem.supplier = tempList[i].supplier;
                    tempList1.Add(tempItem);
                }
            }

            tempList = new List<VedomostItem>();
            tempList = tempList1.OrderBy(x => x.group).ToList();

            numberOfValidStrings = tempList.Count;
            #endregion

            #region Добавление названий групп и сортировка внутри группы
            tempList1 = new List<VedomostItem>();
            List<VedomostItem> groupList = new List<VedomostItem>();
            string prevGroup = tempList[0].group;
            groupList.Add(tempList[0]);

            for (int i = 1; i < numberOfValidStrings; i++)
            {
                if (prevGroup == tempList[i].group)
                {
                    groupList.Add(tempList[i]);
                }
                else if (prevGroup != tempList[i].group)
                {
                    tempItem = new VedomostItem();
                    tempItem.makeEmpty();
                    tempList1.Add(tempItem);//добавляем пустую строчку
                    tempItem = new VedomostItem();
                    tempItem.makeEmpty();
                    //Добавляем название группы
                    if (groupList.Count > 1 || !bAllowSingleLineGroups)
                    {
                        tempItem.name = groupList[0].groupPlural;
                        tempItem.isNameUnderlined = true;
                        tempList1.Add(tempItem);
                        groupList = groupList.OrderBy(x => x.name).ToList();
                        for (int j = 0; j < groupList.Count; j++) tempList1.Add(groupList[j]);
                    }
                    else if (groupList.Count == 1)
                    {
                        string name = groupList[0].group + ' ' + groupList[0].name;
                        if (name.Length < maxNameLength) groupList[0].name = name;
                        else
                        {
                            tempItem.name = groupList[0].group;
                            tempList1.Add(tempItem);
                        }
                        tempList1.Add(groupList[0]);
                    }

                    groupList = new List<VedomostItem>();
                    groupList.Add(tempList[i]);
                    prevGroup = tempList[i].group;
                }

                //Для последнего элемента
                if (i == numberOfValidStrings - 1)
                {
                    tempItem = new VedomostItem();
                    tempItem.makeEmpty();
                    tempList1.Add(tempItem);//добавляем пустую строчку
                    tempItem = new VedomostItem();
                    tempItem.makeEmpty();
                    if (groupList.Count > 1 || !bAllowSingleLineGroups)
                    {
                        tempItem.name = groupList[0].groupPlural;
                        tempItem.isNameUnderlined = true;
                        tempList1.Add(tempItem);
                        groupList = groupList.OrderBy(x => x.name).ToList();
                        for (int j = 0; j < groupList.Count; j++) tempList1.Add(groupList[j]);
                    }
                    else if (groupList.Count == 1)
                    {
                        tempItem.name = groupList[0].group;
                        tempList1.Add(tempItem);
                        tempList1.Add(groupList[0]);
                    }
                }

            }
            tempList = new List<VedomostItem>();
            tempList = tempList1;
            numberOfValidStrings = tempList.Count;
            #endregion

            #region Разбиение каждой записи на нужное количество строк
            List<VedomostItem> tempList2 = new List<VedomostItem>();


            for (int i = 0; i < numberOfValidStrings; i++)
            {
                if (tempList[i].docum != String.Empty)
                {
                    //Разбиение строк, чтобы все надписи вмещались в ячейки.

                    string name = tempList[i].name;

                    while (name != String.Empty)
                    {
                        tempItem = new VedomostItem();
                        tempItem.makeEmpty();
                        tempItem.group = tempList[i].group;
                        tempItem.groupPlural = tempList[i].groupPlural;

                        if (tempList[i].name == name) // если это первая строка записи
                        {
                            tempItem.docum = tempList[i].docum;
                            tempItem.quantityIzdelie = tempList[i].quantityIzdelie;
                            tempItem.quantityTotal = tempList[i].quantityTotal;
                            tempItem.note = tempList[i].note;
                            tempItem.auxNote = tempList[i].auxNote;
                            tempItem.supplier = tempList[i].supplier;
                        }

                        //Разбираемся с наименованием
                        tempItem.name = Global.ParseItersTillLen(ref name, maxNameLength, " ", true);
                        tempList2.Add(tempItem);
                    }
                }
                else
                {
                    tempItem = new VedomostItem();
                    tempItem = tempList[i];
                    tempList2.Add(tempItem);
                }

            }

            numberOfValidStrings = tempList2.Count();

            #endregion

            return tempList2;
        }

    }
}
