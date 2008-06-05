/*
	grafiti library

    Copyright 2008  Alessandro De Nardi <alessandro.denardi@gmail.com>



    This program is free software; you can redistribute it and/or

    modify it under the terms of the GNU General Public License as

    published by the Free Software Foundation; either version 3 of 

    the License, or (at your option) any later version.



    This program is distributed in the hope that it will be useful,

    but WITHOUT ANY WARRANTY; without even the implied warranty of

    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the

    GNU General Public License for more details.



    You should have received a copy of the GNU General Public License

    along with this program.  If not, see <http://www.gnu.org/licenses/>.


*/



using System;

using System.Collections.Generic;



namespace grafiti

{

    public class DoubleDictionary<KeyT1, KeyT2, ValueT>

    {

        private readonly int m_initialCapacity;

        private readonly Dictionary<KeyT1, Dictionary<KeyT2, ValueT>> m_table1;



        public DoubleDictionary() : this(100) { }



        public DoubleDictionary(int capacity)

        {

            m_initialCapacity = capacity;

            m_table1 = new Dictionary<KeyT1, Dictionary<KeyT2, ValueT>>(m_initialCapacity);

        }



        public void Remove(KeyT1 key1, KeyT2 key2)

        {

            m_table1[key1].Remove(key2);

            if (m_table1[key1].Count == 0)

                m_table1.Remove(key1);

        }



        public ValueT this[KeyT1 key1, KeyT2 key2]

        {

            get

            {

                return m_table1[key1][key2];

            }

            set

            {

                Dictionary<KeyT2, ValueT> table2;

                if (!m_table1.ContainsKey(key1))

                {

                    table2 = new Dictionary<KeyT2, ValueT>();

                    m_table1[key1] = table2;

                    table2[key2] = value;

                }

                else

                    m_table1[key1][key2] = value;

            }

        }



        public bool ContainsKeys(KeyT1 key1, KeyT2 key2)

        {

            if (!m_table1.ContainsKey(key1))

                return false;

            return m_table1[key1].ContainsKey(key2);

        }

    }



    public class SimmetricDoubleDictionary<KeyT, ValueT> where KeyT : IComparable

    {

        private readonly int m_initialCapacity;

        private readonly Dictionary<KeyT, Dictionary<KeyT, ValueT>> m_table1;



        public SimmetricDoubleDictionary() : this(100) { }



        public SimmetricDoubleDictionary(int capacity)

        {

            m_initialCapacity = capacity;

            m_table1 = new Dictionary<KeyT, Dictionary<KeyT, ValueT>>(m_initialCapacity);

        }



        public void Remove(KeyT key)

        {

            foreach (KeyT key1 in m_table1.Keys)

            {

                if (key1.CompareTo(key) <= 0)

                    m_table1[key1].Remove(key);

            }

            m_table1.Remove(key);

        }



        public ValueT this[KeyT key1, KeyT key2]

        {

            get

            {

                if (key1.CompareTo(key2) > 0)

                {

                    KeyT temp = key1;

                    key1 = key2;

                    key2 = temp;

                }



                return m_table1[key1][key2];

            }

            set

            {

                if (key1.CompareTo(key2) > 0)

                {

                    KeyT temp = key1;

                    key1 = key2;

                    key2 = temp;

                }



                Dictionary<KeyT, ValueT> table2;

                if (!m_table1.ContainsKey(key1))

                {

                    table2 = new Dictionary<KeyT, ValueT>();

                    m_table1[key1] = table2;

                    table2[key2] = value;

                }

                else

                    m_table1[key1][key2] = value;

            }

        }

    } 

}