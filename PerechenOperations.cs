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
using System.Reflection;
using System.Windows;
using DocGOST.Data;

namespace DocGOST
{
    class PerechenOperations
    {
        const int perech_first_page_rows_count = 23; // число строк в перечне на 1-ом листе
        const int perech_subseq_page_rows_count = 29;// -//- на остальных листах

        public static bool IsLineLastOnPage(int line) {
            return (line == perech_first_page_rows_count) || ((line - perech_first_page_rows_count) % perech_subseq_page_rows_count == 0);
        }

        private long GetDesignatorValue(string designator)
        {
            return Global.GetDesignatorValue(designator);
        }

        public void groupPerechenElements(ref List<PerechenItem> pData, ref int numberOfValidStrings)
        {
            #region Группировка элементов перечня с одинаковым наименованием            
            int numOfSameElems = 1;
            string prevElemName = pData[0].name;
            string prevElemNote = pData[0].note;
            /*
             * Необходимо учитывать позиционное обозначение в том случае, 
             * когда, например, устанавливаются одинаковые индуктивности L1 и L3-L5, а L2 не устанавливается.
             * Если при группировке учитыать только наиметование, то в этом случае они сгруппировались бы как L1-L5.
             * А с учётом позиционного обозначения они сгруппируются как 2 строки: L1 и L3-L5.
             */
            long prevDesignatorValue = GetDesignatorValue(pData[0].designator);

            for (int i = 1; i < numberOfValidStrings; i++)
            {
                long designatorValue = GetDesignatorValue(pData[i].designator);
                if (((pData[i].name == prevElemName) && (pData[i].note == prevElemNote))&&(designatorValue == prevDesignatorValue+1)) numOfSameElems++;
                else
                {
                    prevElemName = pData[i].name;
                    prevElemNote = pData[i].note;

                    if (numOfSameElems > 1)
                    {
                        //Группировка позиционных обозначений:
                        if (numOfSameElems == 2) pData[i - 2].designator += ", " + pData[i - 1].designator;
                        else pData[i - numOfSameElems].designator += "..." + pData[i - 1].designator;
                        // Изменение количества элементов:
                        pData[i - numOfSameElems].quantity = numOfSameElems.ToString();
                        //Перенос оставшихся строчек выше:
                        for (int j = i; j < numberOfValidStrings; j++)
                            pData[j - (numOfSameElems - 1)] = pData[j];


                        //Изменение общего количества строчек, которые нужно записать в перечень:
                        numberOfValidStrings -= (numOfSameElems - 1);
                        i -= (numOfSameElems - 1);

                        numOfSameElems = 1;
                    }

                }
                if ((i == numberOfValidStrings - 1) && (numOfSameElems > 1))
                {

                    //Группировка позиционных обозначений:
                    if (numOfSameElems == 2) pData[i - 1].designator += ", " + pData[i].designator;
                    else pData[i - numOfSameElems + 1].designator += "..." + pData[i].designator;
                    // Изменение количества элементов:
                    pData[i - numOfSameElems + 1].quantity = numOfSameElems.ToString();

                    //Очистка последующей строки:
                    pData[i - numOfSameElems + 2] = new Data.PerechenItem();

                    //Изменение общего количества строчек, которые нужно записать в перечень:
                    numberOfValidStrings -= (numOfSameElems - 1);
                }

                prevDesignatorValue = designatorValue;
            }
            #endregion
            
            #region Группировка элементов перечня по типу и документу           


            // Группировка элементов по типу и документу в текущей версии программы НЕ РЕАЛИЗОВАНА
            // Группировка элементов только по группе

            int numOfSameGroupElems = 1;
            string prevElemGroup = pData[0].groupPlural;
            int stringsAdded = 0;
            int numberOfValidStringsTemp = numberOfValidStrings;
            for (int i = 1; i < numberOfValidStringsTemp; i++)
            {
                if ((pData[i + stringsAdded].groupPlural == prevElemGroup)) numOfSameGroupElems++;
                else
                {
                    if ((numOfSameGroupElems >= 3) && (prevElemGroup != String.Empty))
                    {
                        //Сдвиг находящихся ниже строк на 2 для пустой строки и строки под наименование группы:
                        int sdvigNumber = 2;

                        //Чтобы название группы не было оторвано от первого элемента группы, добавляем ещё строку, если группа начинается на предпоследней строке страницы:
                        if ((i + stringsAdded - numOfSameGroupElems + 2 == perech_first_page_rows_count) | (((i + stringsAdded - numOfSameGroupElems - perech_first_page_rows_count + 2) % perech_subseq_page_rows_count) == 0)) sdvigNumber = 3;

                        //Добавляем необходимое количество строк
                        for (int j = 0; j <= sdvigNumber; j++) pData.Add(new Data.PerechenItem());

                        //Сдвиг находящихся ниже строк для пустой строки и  строки под наименование группы:
                        for (int j = numberOfValidStrings + 1; j <= numberOfValidStrings + sdvigNumber; j++)
                            pData[j] = new Data.PerechenItem();


                        for (int j = numberOfValidStrings; j >= i + stringsAdded - numOfSameGroupElems; j--)
                            pData[j + sdvigNumber] = pData[j];


                        //Освобождение строк:
                        for (int j = i + stringsAdded - numOfSameGroupElems; j < i + stringsAdded - numOfSameGroupElems + sdvigNumber; j++)
                            pData[j] = new Data.PerechenItem();

                        //Добавление названия группы на освобождённую строку:
                        {
                            var idx = i + stringsAdded - numOfSameGroupElems + sdvigNumber - 1;
                            pData[idx].name = prevElemGroup;
                            pData[idx].isNameUnderlined = true;
                        }

                        //Изменение общего количества строчек, которые нужно записать в перечень:
                        numberOfValidStrings += sdvigNumber;
                        stringsAdded += sdvigNumber;


                    }
                    else
                    {
                        //Сдвиг находящихся ниже строк на 1 для пустой строки
                        pData.Add(new Data.PerechenItem());
                        pData.Add(new Data.PerechenItem());

                        for (int j = numberOfValidStrings; j >= i + stringsAdded - numOfSameGroupElems; j--)
                            pData[j + 1] = pData[j];

                        //Освобождение строки:
                        pData[i + stringsAdded - numOfSameGroupElems] = new Data.PerechenItem();

                        //Добавление названия группы к названию каждого несгруппированного элемента:
                        for (int j = i + stringsAdded; j >= i + stringsAdded - numOfSameGroupElems; j--)
                            if (j >= 0) pData[j].name = pData[j].group + ' ' + pData[j].name;


                        //Изменение общего количества строчек, которые нужно записать в перечень:
                        numberOfValidStrings += 1;
                        stringsAdded++;



                    }
                    numOfSameGroupElems = 1;

                    prevElemGroup = pData[i + stringsAdded].groupPlural;
                }

                if (i == (numberOfValidStringsTemp - 1))
                {

                    if ((numOfSameGroupElems >= 2) && (prevElemGroup != String.Empty))
                    {
                        //Сдвиг находящихся ниже строк на 2 для пустой строки и строки под наименование группы:
                        int sdvigNumber = 2;

                        //Чтобы название группы не было оторвано от первого элемента группы, добавляем ещё строку, если группа начинается на предпоследней строке страницы:
                        if ((i + stringsAdded - numOfSameGroupElems + 2 == perech_first_page_rows_count) | (((i + stringsAdded - numOfSameGroupElems - perech_first_page_rows_count + 2) % perech_subseq_page_rows_count) == 0)) sdvigNumber = 3;

                        //Добавляем необходимое количество строк
                        for (int j = 0; j <= sdvigNumber; j++) pData.Add(new Data.PerechenItem());

                        //Сдвиг находящихся ниже строк для пустой строки и  строки под наименование группы:
                        for (int j = numberOfValidStrings; j <= numberOfValidStrings + sdvigNumber; j++)
                            pData[j] = new Data.PerechenItem();

                        for (int j = numberOfValidStrings; j >= i + stringsAdded - numOfSameGroupElems + 1; j--)
                            pData[j + sdvigNumber] = pData[j];

                        //Освобождение строк:
                        for (int j = i + stringsAdded - numOfSameGroupElems + 1; j < i + stringsAdded - numOfSameGroupElems + sdvigNumber + 1; j++)
                            pData[j] = new Data.PerechenItem();

                        //Добавление названия группы на освобождённую строку:
                        {   
                            var idx = i + stringsAdded - numOfSameGroupElems + sdvigNumber;
                            pData[idx].name = prevElemGroup;
                            pData[idx].isNameUnderlined = true;
                        }

                        //Изменение общего количества строчек, которые нужно записать в перечень:
                        numberOfValidStrings += sdvigNumber;
                        stringsAdded += sdvigNumber;
                    }
                    else
                    {
                        //Сдвиг находящихся ниже строк на 1 для пустой строки
                        pData.Add(new Data.PerechenItem());
                        pData.Add(new Data.PerechenItem());

                        for (int j = numberOfValidStrings; j >= i + stringsAdded - numOfSameGroupElems + 1; j--)
                            pData[j + 1] = pData[j];

                        //Освобождение строки:
                        pData[i + stringsAdded - numOfSameGroupElems + 1] = new Data.PerechenItem();

                        //Добавление названия группы к названию каждого несгруппированного элемента:
                        for (int j = i + stringsAdded + 1; j >= i + stringsAdded - numOfSameGroupElems + 1; j--)
                            if (j >= 0) pData[j].name = pData[j].group + ' ' + pData[j].name;

                        //Изменение общего количества строчек, которые нужно записать в перечень:
                        numberOfValidStrings += 1;
                        stringsAdded++;

                    }
                    numOfSameGroupElems = 1;

                    prevElemGroup = pData[i + stringsAdded].groupPlural;
                }
            }

            #endregion

        }

        public static List<PerechenItem> groupPerechenElements_v2(List<PerechenItem> pData) {
            
            var listpi = new List<PerechenItem>(pData.Count);

            for (int i = 0; i < pData.Count; i++) {
                if (i == 0 
                        || pData[i].name != pData[i-1].name || pData[i].note != pData[i - 1].note 
                        || Global.GetDesignatorValue(pData[i].designator) != Global.GetDesignatorValue(pData[i-1].designator)+1)
                    listpi.Add( pData[i] );
                else { 
                    listpi.Last().quantity = (Int32.Parse(listpi.Last().quantity) + Int32.Parse(pData[i].quantity)).ToString();
                }
            }

            var rslt = new List<PerechenItem>(pData.Count);
            for (int i = 0; i < listpi.Count; i++) {
                var curItem = listpi[i];
                var cnt = Int32.Parse(curItem.quantity);
                if (cnt > 1) {
                    var ides = Global.GetDesignatorValue(curItem.designator);
                    var desgn = Global.ExtractDesignatorGroupName(ides);
                    if (cnt == 2)
                        curItem.designator = desgn + Global.ExtractDesignatorSelfNum(ides) + ", " + desgn + Global.ExtractDesignatorSelfNum(ides+1);
                    else
                        curItem.designator = desgn + Global.ExtractDesignatorSelfNum(ides) + "..." + desgn + Global.ExtractDesignatorSelfNum(ides+cnt-1);
                }

                if (i == 0 || curItem.group != listpi[i-1].group) {
                    rslt.Add( new PerechenItem() );
                    if ( (i == listpi.Count - 1) || (curItem.group != listpi[i+1].group) ) {
                        curItem.name = (cnt == 1 ? curItem.group : curItem.groupPlural) + " " + curItem.name;
                        rslt.Add(curItem);
                    }
                    else {
                        rslt.Add(new PerechenItem() {
                            name = curItem.groupPlural,
                            isNameUnderlined = true,
                        });
                        rslt.Add(curItem);
                    }
                }
                else {
                    rslt.Add(curItem);
                }
            } // for
            return rslt;
        }

    } // class
}
