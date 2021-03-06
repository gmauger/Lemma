﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using ComponentBind;
using System.Xml.Serialization;

namespace Lemma.Components
{
	public class Updater : Component<Main>, IUpdateableComponent
	{
		[XmlIgnore]
		public Action<float> Action;

		public Updater()
		{
			this.EnabledInEditMode = false;
		}

		public Updater(Action<float> action = null)
		{
			this.Action = action;
		}

		public override void Awake()
		{
			base.Awake();
			this.Serialize = false;
		}

		public override Entity Entity
		{
			get
			{
				return base.Entity;
			}
			set
			{
				base.Entity = value;
				this.EnabledWhenPaused = false;
			}
		}

		public void Update(float elapsedTime)
		{
			this.Action(elapsedTime);
		}
	}
}
