﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Lemma.Util;
using System.Xml.Serialization;
using Lemma.Factories;
using ComponentBind;

namespace Lemma.Components
{
	public class Rift : Component<Main>, IUpdateableComponent
	{
		private const float damageTime = 1.0f; // How long the player can stand in a rift before they die
		private const float interval = 0.015f; // A coordinate is emptied every x seconds
		public Property<int> Radius = new Property<int> { Value = 10 };
		public Property<float> CurrentRadius = new Property<float> { Editable = false };
		public Property<int> CurrentIndex = new Property<int> { Editable = false };
		public Property<Entity.Handle> Voxel = new Property<Entity.Handle> { Editable = false };
		public Property<Voxel.Coord> Coordinate = new Property<Voxel.Coord> { Editable = false };
		public Property<Vector3> Position = new Property<Vector3> { Editable = false };
		public ListProperty<Voxel.Coord> Coords = new ListProperty<Voxel.Coord> { Editable = false };

		private Voxel voxel;
		private float intervalTimer;

		public override void Awake()
		{
			base.Awake();
			this.EnabledInEditMode = false;
			this.EnabledWhenPaused = false;
			this.Add(new CommandBinding(this.OnEnabled, delegate()
			{
				if (this.Coords.Count == 0)
				{
					if (PlayerFactory.Instance != null)
						PlayerFactory.Instance.Get<CameraController>().Shake.Execute(this.Position, 50.0f);
					Entity voxelEntity = this.Voxel.Value.Target;
					if (voxelEntity != null && voxelEntity.Active)
					{
						Voxel v = voxelEntity.Get<Voxel>();
						Voxel.Coord center = this.Coordinate;
						Vector3 pos = v.GetRelativePosition(center);
						int radius = this.Radius;
						List<VoxelFillFactory.CoordinateEntry> coords = new List<VoxelFillFactory.CoordinateEntry>();
						for (Voxel.Coord x = center.Move(Direction.NegativeX, radius); x.X < center.X + radius; x.X++)
						{
							for (Voxel.Coord y = x.Move(Direction.NegativeY, radius); y.Y < center.Y + radius; y.Y++)
							{
								for (Voxel.Coord z = y.Move(Direction.NegativeZ, radius); z.Z < center.Z + radius; z.Z++)
								{
									float distance = (pos - v.GetRelativePosition(z)).Length();
									if (distance <= radius && v[z] != Components.Voxel.EmptyState)
										coords.Add(new VoxelFillFactory.CoordinateEntry { Coord = z.Clone(), Distance = distance });
								}
							}
						}
						coords.Sort(new LambdaComparer<VoxelFillFactory.CoordinateEntry>((x, y) => x.Distance.CompareTo(y.Distance)));
						this.Coords.AddAll(coords.Select(x => x.Coord));
					}
				}
			}));
		}

		private ImplodeBlockFactory blockFactory = Factory.Get<ImplodeBlockFactory>();

		public void Update(float dt)
		{
			if (this.CurrentIndex < this.Coords.Count)
			{
				if (this.voxel == null)
				{
					Entity v = this.Voxel.Value.Target;
					if (v != null && v.Active)
						this.voxel = v.Get<Voxel>();
				}
				else if (this.voxel.Active)
				{
					this.intervalTimer += dt;
					bool regenerate = false;
					while (this.intervalTimer > interval && this.CurrentIndex < this.Coords.Count)
					{
						Voxel.Coord c = this.Coords[this.CurrentIndex];
						Voxel.State state;
						if ((state = this.voxel[c]) != Components.Voxel.EmptyState)
						{
							this.voxel.Empty(c, true, true);
							regenerate = true;
							this.blockFactory.Implode(main, this.voxel, c, state, this.Position);
						}
						this.CurrentIndex.Value++;
						this.intervalTimer -= interval;
					}
					this.CurrentRadius.Value = (this.voxel.GetRelativePosition(this.Coords[Math.Max(0, this.CurrentIndex - 1)]) - this.voxel.GetRelativePosition(this.Coordinate)).Length();
					if (regenerate)
						this.voxel.Regenerate();
				}
				else
					this.voxel = null;
			}

			Entity player = PlayerFactory.Instance;
			if (player != null && (player.Get<Transform>().Position.Value - this.Position.Value).Length() <= this.CurrentRadius)
				player.Get<Player>().Health.Value -= dt * damageTime;
		}

		public static void AttachEditorComponents(Entity entity, Main main, Vector3 color)
		{
			Property<bool> selected = entity.GetOrMakeProperty<bool>("EditorSelected");
			selected.Serialize = false;

			Rift rift = entity.Get<Rift>();

			ModelAlpha model = new ModelAlpha();
			model.Filename.Value = "Models\\alpha-sphere";
			model.Alpha.Value = 0.15f;
			model.Color.Value = color;
			model.DisableCulling.Value = true;
			model.Add(new Binding<Vector3, int>(model.Scale, x => new Vector3(x), rift.Radius));
			model.Editable = false;
			model.Serialize = false;
			model.DrawOrder.Value = 11; // In front of water
			model.Add(new Binding<bool>(model.Enabled, selected));

			entity.Add(model);

			model.Add(new Binding<Matrix, Vector3>(model.Transform, x => Matrix.CreateTranslation(x), rift.Position));
		}
	}
}
