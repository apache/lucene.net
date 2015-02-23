﻿/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 *      http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Analysis.Compound.Hyphenation
{


	/// <summary>
	/// <h2>Ternary Search Tree.</h2>
	/// 
	/// <para>
	/// A ternary search tree is a hybrid between a binary tree and a digital search
	/// tree (trie). Keys are limited to strings. A data value of type char is stored
	/// in each leaf node. It can be used as an index (or pointer) to the data.
	/// Branches that only contain one key are compressed to one node by storing a
	/// pointer to the trailer substring of the key. This class is intended to serve
	/// as base class or helper class to implement Dictionary collections or the
	/// like. Ternary trees have some nice properties as the following: the tree can
	/// be traversed in sorted order, partial matches (wildcard) can be implemented,
	/// retrieval of all keys within a given distance from the target, etc. The
	/// storage requirements are higher than a binary tree but a lot less than a
	/// trie. Performance is comparable with a hash table, sometimes it outperforms a
	/// hash function (most of the time can determine a miss faster than a hash).
	/// </para>
	/// 
	/// <para>
	/// The main purpose of this java port is to serve as a base for implementing
	/// TeX's hyphenation algorithm (see The TeXBook, appendix H). Each language
	/// requires from 5000 to 15000 hyphenation patterns which will be keys in this
	/// tree. The strings patterns are usually small (from 2 to 5 characters), but
	/// each char in the tree is stored in a node. Thus memory usage is the main
	/// concern. We will sacrifice 'elegance' to keep memory requirements to the
	/// minimum. Using java's char type as pointer (yes, I know pointer it is a
	/// forbidden word in java) we can keep the size of the node to be just 8 bytes
	/// (3 pointers and the data char). This gives room for about 65000 nodes. In my
	/// tests the english patterns took 7694 nodes and the german patterns 10055
	/// nodes, so I think we are safe.
	/// </para>
	/// 
	/// <para>
	/// All said, this is a map with strings as keys and char as value. Pretty
	/// limited!. It can be extended to a general map by using the string
	/// representation of an object and using the char value as an index to an array
	/// that contains the object values.
	/// </para>
	/// 
	/// This class has been taken from the Apache FOP project (http://xmlgraphics.apache.org/fop/). They have been slightly modified. 
	/// </summary>

	public class TernaryTree : ICloneable
	{

	  /// <summary>
	  /// We use 4 arrays to represent a node. I guess I should have created a proper
	  /// node class, but somehow Knuth's pascal code made me forget we now have a
	  /// portable language with virtual memory management and automatic garbage
	  /// collection! And now is kind of late, furthermore, if it ain't broken, don't
	  /// fix it.
	  /// </summary>

	  /// <summary>
	  /// Pointer to low branch and to rest of the key when it is stored directly in
	  /// this node, we don't have unions in java!
	  /// </summary>
	  protected internal char[] lo;

	  /// <summary>
	  /// Pointer to high branch.
	  /// </summary>
	  protected internal char[] hi;

	  /// <summary>
	  /// Pointer to equal branch and to data when this node is a string terminator.
	  /// </summary>
	  protected internal char[] eq;

	  /// <summary>
	  /// <P>
	  /// The character stored in this node: splitchar. Two special values are
	  /// reserved:
	  /// </P>
	  /// <ul>
	  /// <li>0x0000 as string terminator</li>
	  /// <li>0xFFFF to indicate that the branch starting at this node is compressed</li>
	  /// </ul>
	  /// <para>
	  /// This shouldn't be a problem if we give the usual semantics to strings since
	  /// 0xFFFF is guaranteed not to be an Unicode character.
	  /// </para>
	  /// </summary>
	  protected internal char[] sc;

	  /// <summary>
	  /// This vector holds the trailing of the keys when the branch is compressed.
	  /// </summary>
	  protected internal CharVector kv;

	  protected internal char root;

	  protected internal char freenode;

	  protected internal int length; // number of items in tree

	  protected internal const int BLOCK_SIZE = 2048; // allocation size for arrays

	  internal TernaryTree()
	  {
		init();
	  }

	  protected internal virtual void init()
	  {
		root = (char)0;
		freenode = (char)1;
		length = 0;
		lo = new char[BLOCK_SIZE];
		hi = new char[BLOCK_SIZE];
		eq = new char[BLOCK_SIZE];
		sc = new char[BLOCK_SIZE];
		kv = new CharVector();
	  }

	  /// <summary>
	  /// Branches are initially compressed, needing one node per key plus the size
	  /// of the string key. They are decompressed as needed when another key with
	  /// same prefix is inserted. This saves a lot of space, specially for long
	  /// keys.
	  /// </summary>
	  public virtual void insert(string key, char val)
	  {
		// make sure we have enough room in the arrays
		int len = key.Length + 1; // maximum number of nodes that may be generated
		if (freenode + len > eq.Length)
		{
		  redimNodeArrays(eq.Length + BLOCK_SIZE);
		}
		char[] strkey = new char[len--];
		key.CopyTo(0, strkey, 0, len - 0);
		strkey[len] = (char)0;
		root = insert(root, strkey, 0, val);
	  }

	  public virtual void insert(char[] key, int start, char val)
	  {
		int len = strlen(key) + 1;
		if (freenode + len > eq.Length)
		{
		  redimNodeArrays(eq.Length + BLOCK_SIZE);
		}
		root = insert(root, key, start, val);
	  }

	  /// <summary>
	  /// The actual insertion function, recursive version.
	  /// </summary>
	  private char insert(char p, char[] key, int start, char val)
	  {
		int len = strlen(key, start);
		if (p == 0)
		{
		  // this means there is no branch, this node will start a new branch.
		  // Instead of doing that, we store the key somewhere else and create
		  // only one node with a pointer to the key
		  p = freenode++;
		  eq[p] = val; // holds data
		  length++;
		  hi[p] = (char)0;
		  if (len > 0)
		  {
			sc[p] = (char)0xFFFF; // indicates branch is compressed
			lo[p] = (char) kv.alloc(len + 1); // use 'lo' to hold pointer to key
			strcpy(kv.Array, lo[p], key, start);
		  }
		  else
		  {
			sc[p] = (char)0;
			lo[p] = (char)0;
		  }
		  return p;
		}

		if (sc[p] == 0xFFFF)
		{
		  // branch is compressed: need to decompress
		  // this will generate garbage in the external key array
		  // but we can do some garbage collection later
		  char pp = freenode++;
		  lo[pp] = lo[p]; // previous pointer to key
		  eq[pp] = eq[p]; // previous pointer to data
		  lo[p] = (char)0;
		  if (len > 0)
		  {
			sc[p] = kv.get(lo[pp]);
			eq[p] = pp;
			lo[pp]++;
			if (kv.get(lo[pp]) == 0)
			{
			  // key completly decompressed leaving garbage in key array
			  lo[pp] = (char)0;
			  sc[pp] = (char)0;
			  hi[pp] = (char)0;
			}
			else
			{
			  // we only got first char of key, rest is still there
			  sc[pp] = (char)0xFFFF;
			}
		  }
		  else
		  {
			// In this case we can save a node by swapping the new node
			// with the compressed node
			sc[pp] = (char)0xFFFF;
			hi[p] = pp;
			sc[p] = (char)0;
			eq[p] = val;
			length++;
			return p;
		  }
		}
		char s = key[start];
		if (s < sc[p])
		{
		  lo[p] = insert(lo[p], key, start, val);
		}
		else if (s == sc[p])
		{
		  if (s != 0)
		  {
			eq[p] = insert(eq[p], key, start + 1, val);
		  }
		  else
		  {
			// key already in tree, overwrite data
			eq[p] = val;
		  }
		}
		else
		{
		  hi[p] = insert(hi[p], key, start, val);
		}
		return p;
	  }

	  /// <summary>
	  /// Compares 2 null terminated char arrays
	  /// </summary>
	  public static int strcmp(char[] a, int startA, char[] b, int startB)
	  {
		for (; a[startA] == b[startB]; startA++, startB++)
		{
		  if (a[startA] == 0)
		  {
			return 0;
		  }
		}
		return a[startA] - b[startB];
	  }

	  /// <summary>
	  /// Compares a string with null terminated char array
	  /// </summary>
	  public static int strcmp(string str, char[] a, int start)
	  {
		int i , d , len = str.Length;
		for (i = 0; i < len; i++)
		{
		  d = (int) str[i] - a[start + i];
		  if (d != 0)
		  {
			return d;
		  }
		  if (a[start + i] == 0)
		  {
			return d;
		  }
		}
		if (a[start + i] != 0)
		{
		  return -a[start + i];
		}
		return 0;

	  }

	  public static void strcpy(char[] dst, int di, char[] src, int si)
	  {
		while (src[si] != 0)
		{
		  dst[di++] = src[si++];
		}
		dst[di] = (char)0;
	  }

	  public static int strlen(char[] a, int start)
	  {
		int len = 0;
		for (int i = start; i < a.Length && a[i] != 0; i++)
		{
		  len++;
		}
		return len;
	  }

	  public static int strlen(char[] a)
	  {
		return strlen(a, 0);
	  }

	  public virtual int find(string key)
	  {
		int len = key.Length;
		char[] strkey = new char[len + 1];
		key.CopyTo(0, strkey, 0, len - 0);
		strkey[len] = (char)0;

		return find(strkey, 0);
	  }

	  public virtual int find(char[] key, int start)
	  {
		int d;
		char p = root;
		int i = start;
		char c;

		while (p != 0)
		{
		  if (sc[p] == 0xFFFF)
		  {
			if (strcmp(key, i, kv.Array, lo[p]) == 0)
			{
			  return eq[p];
			}
			else
			{
			  return -1;
			}
		  }
		  c = key[i];
		  d = c - sc[p];
		  if (d == 0)
		  {
			if (c == 0)
			{
			  return eq[p];
			}
			i++;
			p = eq[p];
		  }
		  else if (d < 0)
		  {
			p = lo[p];
		  }
		  else
		  {
			p = hi[p];
		  }
		}
		return -1;
	  }

	  public virtual bool knows(string key)
	  {
		return (find(key) >= 0);
	  }

	  // redimension the arrays
	  private void redimNodeArrays(int newsize)
	  {
		int len = newsize < lo.Length ? newsize : lo.Length;
		char[] na = new char[newsize];
		Array.Copy(lo, 0, na, 0, len);
		lo = na;
		na = new char[newsize];
		Array.Copy(hi, 0, na, 0, len);
		hi = na;
		na = new char[newsize];
		Array.Copy(eq, 0, na, 0, len);
		eq = na;
		na = new char[newsize];
		Array.Copy(sc, 0, na, 0, len);
		sc = na;
	  }

	  public virtual int size()
	  {
		return length;
	  }

	  public override TernaryTree clone()
	  {
		TernaryTree t = new TernaryTree();
		t.lo = this.lo.Clone();
		t.hi = this.hi.Clone();
		t.eq = this.eq.Clone();
		t.sc = this.sc.Clone();
		t.kv = this.kv.clone();
		t.root = this.root;
		t.freenode = this.freenode;
		t.length = this.length;

		return t;
	  }

	  /// <summary>
	  /// Recursively insert the median first and then the median of the lower and
	  /// upper halves, and so on in order to get a balanced tree. The array of keys
	  /// is assumed to be sorted in ascending order.
	  /// </summary>
	  protected internal virtual void insertBalanced(string[] k, char[] v, int offset, int n)
	  {
		int m;
		if (n < 1)
		{
		  return;
		}
		m = n >> 1;

		insert(k[m + offset], v[m + offset]);
		insertBalanced(k, v, offset, m);

		insertBalanced(k, v, offset + m + 1, n - m - 1);
	  }

	  /// <summary>
	  /// Balance the tree for best search performance
	  /// </summary>
	  public virtual void balance()
	  {
		// System.out.print("Before root splitchar = ");
		// System.out.println(sc[root]);

		int i = 0, n = length;
		string[] k = new string[n];
		char[] v = new char[n];
		Iterator iter = new Iterator(this);
		while (iter.hasMoreElements())
		{
		  v[i] = iter.Value;
		  k[i++] = iter.nextElement();
		}
		init();
		insertBalanced(k, v, 0, n);

		// With uniform letter distribution sc[root] should be around 'm'
		// System.out.print("After root splitchar = ");
		// System.out.println(sc[root]);
	  }

	  /// <summary>
	  /// Each node stores a character (splitchar) which is part of some key(s). In a
	  /// compressed branch (one that only contain a single string key) the trailer
	  /// of the key which is not already in nodes is stored externally in the kv
	  /// array. As items are inserted, key substrings decrease. Some substrings may
	  /// completely disappear when the whole branch is totally decompressed. The
	  /// tree is traversed to find the key substrings actually used. In addition,
	  /// duplicate substrings are removed using a map (implemented with a
	  /// TernaryTree!).
	  /// 
	  /// </summary>
	  public virtual void trimToSize()
	  {
		// first balance the tree for best performance
		balance();

		// redimension the node arrays
		redimNodeArrays(freenode);

		// ok, compact kv array
		CharVector kx = new CharVector();
		kx.alloc(1);
		TernaryTree map = new TernaryTree();
		compact(kx, map, root);
		kv = kx;
		kv.trimToSize();
	  }

	  private void compact(CharVector kx, TernaryTree map, char p)
	  {
		int k;
		if (p == 0)
		{
		  return;
		}
		if (sc[p] == 0xFFFF)
		{
		  k = map.find(kv.Array, lo[p]);
		  if (k < 0)
		  {
			k = kx.alloc(strlen(kv.Array, lo[p]) + 1);
			strcpy(kx.Array, k, kv.Array, lo[p]);
			map.insert(kx.Array, k, (char) k);
		  }
		  lo[p] = (char) k;
		}
		else
		{
		  compact(kx, map, lo[p]);
		  if (sc[p] != 0)
		  {
			compact(kx, map, eq[p]);
		  }
		  compact(kx, map, hi[p]);
		}
	  }

	  public virtual IEnumerator<string> keys()
	  {
		return new Iterator(this);
	  }

	  public class Iterator : IEnumerator<string>
	  {
		  private readonly TernaryTree outerInstance;


		/// <summary>
		/// current node index
		/// </summary>
		internal int cur;

		/// <summary>
		/// current key
		/// </summary>
		internal string curkey;

		private class Item : ICloneable
		{
			private readonly TernaryTree.Iterator outerInstance;

		  internal char parent;

		  internal char child;

		  public Item(TernaryTree.Iterator outerInstance)
		  {
			  this.outerInstance = outerInstance;
			parent = (char)0;
			child = (char)0;
		  }

		  public Item(TernaryTree.Iterator outerInstance, char p, char c)
		  {
			  this.outerInstance = outerInstance;
			parent = p;
			child = c;
		  }

		  public override Item clone()
		  {
			return new Item(outerInstance, parent, child);
		  }

		}

		/// <summary>
		/// Node stack
		/// </summary>
		internal Stack<Item> ns;

		/// <summary>
		/// key stack implemented with a StringBuilder
		/// </summary>
		internal StringBuilder ks;

		public Iterator(TernaryTree outerInstance)
		{
			this.outerInstance = outerInstance;
		  cur = -1;
		  ns = new Stack<>();
		  ks = new StringBuilder();
		  rewind();
		}

		public virtual void rewind()
		{
		  ns.removeAllElements();
		  ks.Length = 0;
		  cur = outerInstance.root;
		  run();
		}

		public override string nextElement()
		{
		  string res = curkey;
		  cur = up();
		  run();
		  return res;
		}

		public virtual char Value
		{
			get
			{
			  if (cur >= 0)
			  {
				return outerInstance.eq[cur];
			  }
			  return 0;
			}
		}

		public override bool hasMoreElements()
		{
		  return (cur != -1);
		}

		/// <summary>
		/// traverse upwards
		/// </summary>
		internal virtual int up()
		{
		  Item i = new Item(this);
		  int res = 0;

		  if (ns.Count == 0)
		  {
			return -1;
		  }

		  if (cur != 0 && outerInstance.sc[cur] == 0)
		  {
			return outerInstance.lo[cur];
		  }

		  bool climb = true;

		  while (climb)
		  {
			i = ns.Pop();
			i.child++;
			switch (i.child)
			{
			  case 1:
				if (outerInstance.sc[i.parent] != 0)
				{
				  res = outerInstance.eq[i.parent];
				  ns.Push(i.clone());
				  ks.Append(outerInstance.sc[i.parent]);
				}
				else
				{
				  i.child++;
				  ns.Push(i.clone());
				  res = outerInstance.hi[i.parent];
				}
				climb = false;
				break;

			  case 2:
				res = outerInstance.hi[i.parent];
				ns.Push(i.clone());
				if (ks.Length > 0)
				{
				  ks.Length = ks.Length - 1; // pop
				}
				climb = false;
				break;

			  default:
				if (ns.Count == 0)
				{
				  return -1;
				}
				climb = true;
				break;
			}
		  }
		  return res;
		}

		/// <summary>
		/// traverse the tree to find next key
		/// </summary>
		internal virtual int run()
		{
		  if (cur == -1)
		  {
			return -1;
		  }

		  bool leaf = false;
		  while (true)
		  {
			// first go down on low branch until leaf or compressed branch
			while (cur != 0)
			{
			  if (outerInstance.sc[cur] == 0xFFFF)
			  {
				leaf = true;
				break;
			  }
			  ns.Push(new Item(this, (char) cur, '\u0000'));
			  if (outerInstance.sc[cur] == 0)
			  {
				leaf = true;
				break;
			  }
			  cur = outerInstance.lo[cur];
			}
			if (leaf)
			{
			  break;
			}
			// nothing found, go up one node and try again
			cur = up();
			if (cur == -1)
			{
			  return -1;
			}
		  }
		  // The current node should be a data node and
		  // the key should be in the key stack (at least partially)
		  StringBuilder buf = new StringBuilder(ks.ToString());
		  if (outerInstance.sc[cur] == 0xFFFF)
		  {
			int p = outerInstance.lo[cur];
			while (outerInstance.kv.get(p) != 0)
			{
			  buf.Append(outerInstance.kv.get(p++));
			}
		  }
		  curkey = buf.ToString();
		  return 0;
		}

	  }

	  public virtual void printStats(PrintStream @out)
	  {
		@out.println("Number of keys = " + Convert.ToString(length));
		@out.println("Node count = " + Convert.ToString(freenode));
		// System.out.println("Array length = " + Integer.toString(eq.length));
		@out.println("Key Array length = " + Convert.ToString(kv.length()));

		/*
		 * for(int i=0; i<kv.length(); i++) if ( kv.get(i) != 0 )
		 * System.out.print(kv.get(i)); else System.out.println("");
		 * System.out.println("Keys:"); for(Enumeration enum = keys();
		 * enum.hasMoreElements(); ) System.out.println(enum.nextElement());
		 */

	  }
	/*
	  public static void main(String[] args) {
	    TernaryTree tt = new TernaryTree();
	    tt.insert("Carlos", 'C');
	    tt.insert("Car", 'r');
	    tt.insert("palos", 'l');
	    tt.insert("pa", 'p');
	    tt.trimToSize();
	    System.out.println((char) tt.find("Car"));
	    System.out.println((char) tt.find("Carlos"));
	    System.out.println((char) tt.find("alto"));
	    tt.printStats(System.out);
	  }
	  */

	}

}