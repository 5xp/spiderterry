﻿using Sandbox;
using System;

[Library( "webshooter" )]
public partial class WebShooter : Carriable
{
	//public override string ViewModelPath => "models/first_person/first_person_arms.vmdl";

	protected PhysicsBody heldBody;
	protected float holdDistance;
	protected float webLength;
	protected bool grabbing;
	protected float groundFriction = 4.0f;

	protected virtual float MinTargetDistance => 0.0f;
	protected virtual float MaxTargetDistance => 5000.0f;
	protected virtual float TargetDistanceSpeed => 25.0f;
	protected virtual float PullSpeed => 1000.0f;

	public const string GrabbedTag = "grabbed";

	[Net] public bool WebActive { get; set; }
	[Net] public Entity GrabbedEntity { get; set; }
	[Net] public Vector3 GrabbedPos { get; set; }

	public PhysicsBody HeldBody => heldBody;

	public override void Spawn()
	{
		base.Spawn();

		Tags.Add( "weapon" );
		SetModel( "weapons/rust_pistol/rust_pistol.vmdl" );
	}

	public override void Simulate( Client client )
	{
		if ( Owner is not Player owner ) return;

		var eyePos = owner.EyePosition;
		var eyeDir = owner.EyeRotation.Forward;

		if ( Input.Pressed( InputButton.PrimaryAttack ) )
		{
			(Owner as AnimatedEntity)?.SetAnimParameter( "b_attack", true );

			if ( !grabbing )
			{
				grabbing = true;
			}

		}

		bool grabEnabled = grabbing && Input.Down( InputButton.PrimaryAttack );
		bool pullEnabled = grabEnabled && Input.Pressed( InputButton.Jump );

		WebActive = grabEnabled;

		if ( IsServer )
		{
			using ( Prediction.Off() )
			{
				if ( grabEnabled )
				{
					if ( heldBody.IsValid() )
					{
						UpdateGrab( eyePos, pullEnabled );

						if ( pullEnabled )
						{
							GrabEnd();
						}

					}
					else
					{
						TryStartGrab( eyePos, eyeDir );
					}
				}
				else if ( grabbing )
				{
					GrabEnd();

				}
			}
		}


	}

	private void TryStartGrab( Vector3 eyePos, Vector3 eyeDir )
	{
		var tr = Trace.Ray( eyePos, eyePos + eyeDir * MaxTargetDistance )
			.UseHitboxes()
			.WithAnyTags( "solid", "debris" )
			.Ignore( this )
			.Run();

		if ( !tr.Hit || !tr.Entity.IsValid() || tr.StartedSolid ) return;

		var rootEnt = tr.Entity.Root;
		var body = tr.Body;

		if ( !body.IsValid() )
			return;

		//
		// Don't move keyframed, unless it's a player
		//
		if ( body.BodyType == PhysicsBodyType.Keyframed && rootEnt is not Player )
			return;

		GrabInit( body, eyePos, tr.EndPosition );

		GrabbedEntity = rootEnt;

		GrabbedPos = body.Transform.PointToLocal( tr.EndPosition );

	}

	private void UpdateGrab( Vector3 eyePos, bool pulling )
	{
		if ( Owner is not Player owner ) return;

		var controller = owner.GetActiveController() as SpiderController;

		// adjust web length with mouse wheel
		if ( Input.MouseWheel != 0 )
			MoveTargetDistance( Input.MouseWheel * TargetDistanceSpeed );

		float distance = Vector3.DistanceBetween( eyePos, GrabbedPos );

		Vector3 grabDirection = GrabbedPos - eyePos;
		grabDirection = grabDirection.Normal;

		var currentspeed = owner.Velocity.Dot( grabDirection );

		var addspeed = PullSpeed - currentspeed;

		controller.ClearGroundEntity();

		// change speed if pulling in a different direction than velocity
		if ( pulling && addspeed > 0 )
		{
			owner.Velocity += grabDirection * addspeed;
		}

		if ( distance < webLength * 0.7f )
			return;


		addspeed = 100.0f - currentspeed;

		if ( addspeed <= 0 )
			return;

		owner.Velocity += grabDirection * addspeed;


	}

	private void DisableFriction()
	{
		if ( Owner is not Player owner ) return;

		var controller = owner.GetActiveController() as SpiderController;

		controller.GroundFriction = 1.0f;
	}

	private void EnableFriction()
	{
		if ( Owner is not Player owner ) return;
		var controller = owner.GetActiveController() as SpiderController;

		controller.GroundFriction = 4.0f;
	}

	private void Activate()
	{
		if ( !IsServer )
		{
			return;
		}
	}

	private void Deactivate()
	{
		if ( IsServer )
		{
			GrabEnd();
		}

		KillEffects();
	}

	public override void ActiveStart( Entity ent )
	{
		base.ActiveStart( ent );

		Activate();
	}

	public override void ActiveEnd( Entity ent, bool dropped )
	{
		base.ActiveEnd( ent, dropped );

		Deactivate();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		Deactivate();
	}

	public override void OnCarryDrop( Entity dropper )
	{
	}

	private void GrabInit( PhysicsBody body, Vector3 startPos, Vector3 grabPos )
	{
		if ( !body.IsValid() )
			return;

		GrabEnd();

		grabbing = true;
		heldBody = body;
		holdDistance = Vector3.DistanceBetween( startPos, grabPos );
		holdDistance = holdDistance.Clamp( MinTargetDistance, MaxTargetDistance );
		webLength = holdDistance - 10f;

		DisableFriction();
		Sound.FromScreen( "web" );
	}

	private void GrabEnd()
	{
		if ( GrabbedEntity.IsValid() )
		{
			GrabbedEntity = null;
		}

		Sound.FromScreen( "swish" );
		heldBody = null;
		grabbing = false;
		EnableFriction();
	}

	[Event.Physics.PreStep]
	public void OnPrePhysicsStep()
	{
	}

	private void MoveTargetDistance( float distance )
	{
		holdDistance -= distance;
		holdDistance = holdDistance.Clamp( MinTargetDistance, MaxTargetDistance );
		webLength = holdDistance;
	}

	public override void BuildInput( InputBuilder owner )
	{
		if ( !owner.Down( InputButton.Use ) ||
			 !owner.Down( InputButton.PrimaryAttack ) ||
			 !GrabbedEntity.IsValid() )
		{
			return;
		}

		//
		// Lock view angles
		//
		owner.ViewAngles = owner.OriginalViewAngles;
	}

	public override bool IsUsable( Entity user )
	{
		return Owner == null || HeldBody.IsValid();
	}
}
