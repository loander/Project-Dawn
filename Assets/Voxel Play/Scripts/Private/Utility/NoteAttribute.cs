﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	[Serializable]
	public class InspectorNote {
		public byte dummy;
	}

	public class NoteAttribute : PropertyAttribute {
		public string text;
		public float margin;

		public NoteAttribute (string text, float margin = 0) {
			this.text = text;
			this.margin = margin;
		}
	}

}
