namespace Mogo.AI.BT
{
	public sealed class BT2008 : Mogo.AI.BehaviorTreeRoot
	{
		private static BT2008 _instance = null;
		public static BT2008 Instance
		{
			get
			{
				if(_instance == null)
					_instance = new BT2008();

				return _instance;
			}
		}

		private BT2008()
		{
			{
				Mogo.AI.SelectorNode node1 = new Mogo.AI.SelectorNode();
				this.AddChild(node1);
				node1.AddChild(new Mogo.AI.CmpEnemyNum(Mogo.AI.CmpType.eq,0));
				{
					Mogo.AI.SequenceNode node3 = new Mogo.AI.SequenceNode();
					node1.AddChild(node3);
					{
						Mogo.AI.SelectorNode node4 = new Mogo.AI.SelectorNode();
						node3.AddChild(node4);
						node4.AddChild(new Mogo.AI.HasFightTarget());
						node4.AddChild(new Mogo.AI.AOI(0));
					}
					{
						Mogo.AI.Not node7 = new Mogo.AI.Not();
						node3.AddChild(node7);
						node7.Proxy(new Mogo.AI.ISCD());
					}
					node3.AddChild(new Mogo.AI.IsTargetCanBeAttack());
					{
						Mogo.AI.SelectorNode node10 = new Mogo.AI.SelectorNode();
						node3.AddChild(node10);
						{
							Mogo.AI.SequenceNode node11 = new Mogo.AI.SequenceNode();
							node10.AddChild(node11);
							node11.AddChild(new Mogo.AI.InSkillRange(1));
							{
								Mogo.AI.Not node13 = new Mogo.AI.Not();
								node11.AddChild(node13);
								node13.Proxy(new Mogo.AI.InSkillCoolDown(1));
							}
							node11.AddChild(new Mogo.AI.CmpRate(Mogo.AI.CmpType.lt,50));
							node11.AddChild(new Mogo.AI.Escape(1000));
						}
						{
							Mogo.AI.SequenceNode node17 = new Mogo.AI.SequenceNode();
							node10.AddChild(node17);
							node17.AddChild(new Mogo.AI.InSkillCoolDown(6));
							node17.AddChild(new Mogo.AI.InSkillRange(6));
							node17.AddChild(new Mogo.AI.CmpRate(Mogo.AI.CmpType.lt,20));
							node17.AddChild(new Mogo.AI.CastSpell(6,0));
							node17.AddChild(new Mogo.AI.EnterCD(0));
						}
						{
							Mogo.AI.SequenceNode node23 = new Mogo.AI.SequenceNode();
							node10.AddChild(node23);
							node23.AddChild(new Mogo.AI.InSkillCoolDown(5));
							node23.AddChild(new Mogo.AI.InSkillRange(5));
							node23.AddChild(new Mogo.AI.CmpRate(Mogo.AI.CmpType.lt,20));
							node23.AddChild(new Mogo.AI.CastSpell(5,0));
							node23.AddChild(new Mogo.AI.EnterCD(-1500));
						}
						{
							Mogo.AI.SequenceNode node29 = new Mogo.AI.SequenceNode();
							node10.AddChild(node29);
							node29.AddChild(new Mogo.AI.InSkillCoolDown(4));
							node29.AddChild(new Mogo.AI.InSkillRange(4));
							node29.AddChild(new Mogo.AI.CmpRate(Mogo.AI.CmpType.lt,25));
							node29.AddChild(new Mogo.AI.CastSpell(4,0));
							node29.AddChild(new Mogo.AI.EnterCD(200));
						}
						{
							Mogo.AI.SequenceNode node35 = new Mogo.AI.SequenceNode();
							node10.AddChild(node35);
							node35.AddChild(new Mogo.AI.InSkillCoolDown(3));
							node35.AddChild(new Mogo.AI.InSkillRange(3));
							node35.AddChild(new Mogo.AI.CmpRate(Mogo.AI.CmpType.lt,30));
							node35.AddChild(new Mogo.AI.CastSpell(3,0));
							node35.AddChild(new Mogo.AI.EnterCD(-3500));
						}
						{
							Mogo.AI.SequenceNode node41 = new Mogo.AI.SequenceNode();
							node10.AddChild(node41);
							node41.AddChild(new Mogo.AI.InSkillCoolDown(2));
							node41.AddChild(new Mogo.AI.InSkillRange(2));
							node41.AddChild(new Mogo.AI.CmpRate(Mogo.AI.CmpType.lt,30));
							node41.AddChild(new Mogo.AI.CastSpell(2,0));
							node41.AddChild(new Mogo.AI.EnterCD(200));
						}
						{
							Mogo.AI.SequenceNode node47 = new Mogo.AI.SequenceNode();
							node10.AddChild(node47);
							node47.AddChild(new Mogo.AI.InSkillCoolDown(1));
							node47.AddChild(new Mogo.AI.InSkillRange(1));
							node47.AddChild(new Mogo.AI.CmpRate(Mogo.AI.CmpType.lt,80));
							node47.AddChild(new Mogo.AI.CastSpell(1,0));
							node47.AddChild(new Mogo.AI.EnterCD(200));
						}
						{
							Mogo.AI.SequenceNode node53 = new Mogo.AI.SequenceNode();
							node10.AddChild(node53);
							node53.AddChild(new Mogo.AI.CmpRate(Mogo.AI.CmpType.lt,80));
							node53.AddChild(new Mogo.AI.ChooseCastPoint(1));
							node53.AddChild(new Mogo.AI.MoveTo());
						}
						node10.AddChild(new Mogo.AI.EnterRest(500));
					}
				}
			}
		}
	}
}
