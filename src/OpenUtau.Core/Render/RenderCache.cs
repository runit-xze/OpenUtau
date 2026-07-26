using System.Collections.Generic;

namespace OpenUtau.Core.Render {
	class RenderCache {
		class Node {
			public uint hash;
			public byte[] data;

			public Node prev;
			public Node next;
		}

		private readonly long maxBytes;
		private long usedBytes;
		private readonly Node dummyHead;
		private readonly Node dummyTail;
		private readonly Dictionary<uint, Node> dict;
		private readonly object lockObj = new object();

		public RenderCache(long maxBytes) {
			this.maxBytes = maxBytes;
			usedBytes = 0;
			dummyHead = new Node();
			dummyTail = new Node();
			dummyHead.next = dummyTail;
			dummyTail.prev = dummyHead;
			dict = new Dictionary<uint, Node>();
		}

		public byte[] Get(uint hash) {
			lock (lockObj) {
				if (dict.TryGetValue(hash, out Node node)) {
					Remove(node);
					AddToLast(node);
					return node.data;
				}
				return null;
			}
		}

		public void Put(uint hash, byte[] data) {
			lock (lockObj) {
				if (dict.TryGetValue(hash, out Node node)) {
					usedBytes -= node.data.Length;
					node.data = data;
					usedBytes += data.Length;
					Remove(node);
					AddToLast(node);
				} else {
					while (usedBytes + data.Length > maxBytes && dict.Count > 0) {
						Node evict = dummyHead.next;
						usedBytes -= evict.data.Length;
						dict.Remove(evict.hash);
						Remove(evict);
					}
					Node newNode = new Node {
						hash = hash,
						data = data,
					};
					dict.Add(hash, newNode);
					AddToLast(newNode);
					usedBytes += data.Length;
				}
			}
		}

		public void Clear() {
			usedBytes = 0;
			dummyHead.next = dummyTail;
			dummyTail.prev = dummyHead;
			dict.Clear();
		}

		private void Remove(Node node) {
			node.next.prev = node.prev;
			node.prev.next = node.next;
		}

		private void AddToLast(Node node) {
			node.next = dummyTail;
			node.prev = dummyTail.prev;
			dummyTail.prev.next = node;
			dummyTail.prev = node;
		}
	}
}
