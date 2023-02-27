﻿using System;
using EndlessRacer.Career;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EndlessRacer.GameObjects
{
    internal class Player
    {
        private Texture2D _sprite;
        private readonly Texture2D _spriteMove;
        private readonly Texture2D _spriteJump;
        private readonly Texture2D _spriteHurt;
        private readonly Texture2D _spriteVictory;

        private readonly SoundEffect _crashSound;
        private readonly SoundEffect _winSound;

        private Vector2 _position;
        private const double BaseSpeed = 200f;          // speed when idle
        private const double AcceleratedSpeed = 400f;   // speed when holding down move button
        private Vector2 _speed;
        private KeyboardState _ks;

        private PlayerState _currentState;
        private Angle _angle;

        private const double JumpDuration = 1.5;
        private double _jumpTimeRemaining;
        private int MaxAscensionHeight = Constants.TileSize / 2;
        private const double AscensionRate = 100.0;
        private const double DescensionRate = 30.0;
        private double _currentAscension;
        private bool _ascensionReached;

        private const double HurtDuration = 1.5;
        private double _hurtTimeRemaining;

        private const double InvincibleDuration = 1.5;
        private double _invincibleRemaining;
        private int _frame = 0;

        private const double TurnIntervalGround = 0.1f;
        private const double TurnIntervalAir = 0.05f;
        private double _turnInterval;
        private double _turnTimer;
        private bool _canTurn;

        private const int VictoryFrameRate = 30;
        private int _victoryFrame;
        public bool IsVictorious;

        public Player(Vector2 initialPosition, Texture2D spriteMove, Texture2D spriteJump, Texture2D spriteHurt, Texture2D spriteVictory,
            SoundEffect crashSound, SoundEffect winSound)
        {
            _position = initialPosition;
            _currentState = PlayerState.Moving;

            _angle = Angle.Down;

            _spriteMove = spriteMove;
            _spriteJump = spriteJump;
            _spriteHurt = spriteHurt;
            _spriteVictory = spriteVictory;

            _crashSound = crashSound;
            _winSound = winSound;

            ChangeState(PlayerState.Moving);
        }

        public float Update(GameTime gameTime)
        {
            var dt = gameTime.ElapsedGameTime.TotalSeconds;

            _ks = Keyboard.GetState();

            if (_currentState != PlayerState.Hurt)
            {
                if (_ks.IsKeyDown(Controls.TurnLeft)) { Turn(-1); }
                if (_ks.IsKeyDown(Controls.TurnRight)) { Turn(1); }
            }

            double speed;

            if (_ks.IsKeyDown(Controls.Move))
            {
                speed = AcceleratedSpeed;
            }
            else
            {
                speed = BaseSpeed;
            }

            if (_currentState == PlayerState.Moving || _currentState == PlayerState.Invincible)
            {
                Move(speed, gameTime);
            }

            if (_currentState == PlayerState.Invincible)
            {
                _invincibleRemaining -= dt;

                if (_invincibleRemaining <= 0)
                {
                    ChangeState(PlayerState.Moving);
                }
            }

            _position.X += _speed.X;

            if (_currentState == PlayerState.Jumping)
            {
                _jumpTimeRemaining -= dt;

                if (_ascensionReached)
                {
                    _currentAscension -= DescensionRate * dt;

                    if (_currentAscension <= 0)
                    {
                        _currentAscension = 0;
                    }
                }
                else
                {
                    _currentAscension += AscensionRate * dt;

                    if (_currentAscension >= MaxAscensionHeight)
                    {
                        _ascensionReached = true;
                    }
                }

                if (_jumpTimeRemaining <= 0)
                {
                    if (_angle >= Angle.Left && _angle <= Angle.Right)
                    {
                        ChangeState(PlayerState.Moving);
                    }
                    else
                    {
                        ChangeState(PlayerState.Hurt);
                    }
                }
            }

            if (_currentState == PlayerState.Hurt)
            {
                _speed = Vector2.Zero;

                _hurtTimeRemaining -= dt;

                if (_hurtTimeRemaining <= 0)
                {
                    ChangeState(PlayerState.Invincible);
                }
            }

            if (_currentState == PlayerState.Victory)
            {
                _speed = Vector2.Zero;
            }

            if (!_canTurn)
            {
                _turnTimer -= dt;

                if (_turnTimer <= 0)
                {
                    _canTurn = true;
                }
            }

            _frame++;

            if (_frame % VictoryFrameRate == 0)
            {
                _victoryFrame++;
            }

            // Y speed is passed to level to set scroll speed;
            return _speed.Y;
        }

        // turn direction:
        // -1 -> Left
        // +1 -> Right
        private void Turn(int turnDirection)
        {
            if (_canTurn)
            {
                _angle += turnDirection;

                if (_currentState == PlayerState.Moving || _currentState == PlayerState.Invincible)
                {
                    _angle = (Angle)Math.Clamp((int)_angle, (int)Angle.Left, (int)Angle.Right);
                }
                else if (_currentState == PlayerState.Jumping)
                {
                    if (_angle < 0)
                    {
                        _angle = Angle.LeftUp1;
                    }

                    if (_angle > Angle.LeftUp1)
                    {
                        _angle = Angle.Left;
                    }
                }

                _canTurn = false;
                _turnTimer = _turnInterval;
            }
        }

        private void Move(double speed, GameTime gameTime)
        {
            var xIntensity = _angle.ToXIntensity();
            var yIntensity = _angle.ToYIntensity();

            var xSpeed = (float)(xIntensity * speed * gameTime.ElapsedGameTime.TotalSeconds);
            var ySpeed = (float)(yIntensity * speed * gameTime.ElapsedGameTime.TotalSeconds);

            _speed = new Vector2(xSpeed, ySpeed);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            var sourceRectangle = new Rectangle((int)_angle * Constants.TileSize, 0, Constants.TileSize, Constants.TileSize);

            // flicker when invincible
            if (_currentState == PlayerState.Invincible)
            {
                if (_frame % 2 == 0)
                {
                    return;
                }
            }

            if (_currentState == PlayerState.Hurt) // draw without source rectangle if hurt
            {
                spriteBatch.Draw(_sprite, _position, Color.White);
            }
            else if (_currentState == PlayerState.Jumping) // draw ascending/descending sprite
            {
                var position = new Vector2(_position.X, _position.Y - (float)_currentAscension);
                spriteBatch.Draw(_sprite, position, sourceRectangle, Color.White);
            }
            else if (_currentState == PlayerState.Victory) // change source rectangle every few frames
            {
                var index = _victoryFrame % 2 == 0 ? 0 : 1;
                var x = index * Constants.TileSize;
                var y = 0;

                sourceRectangle = new Rectangle(x, y, Constants.TileSize, Constants.TileSize);
                spriteBatch.Draw(_sprite, _position, sourceRectangle, Color.White);
            }
            else
            {
                spriteBatch.Draw(_sprite, _position, sourceRectangle, Color.White);
            }
        }

        public void Jump()
        {
            if (_currentState == PlayerState.Moving || _currentState == PlayerState.Invincible)
            {
                ChangeState(PlayerState.Jumping);
            }
        }

        private void ChangeState(PlayerState newState)
        {
            _currentState = newState;

            switch (newState)
            {
                case PlayerState.Moving:
                    _sprite = _spriteMove;
                    _turnInterval = TurnIntervalGround;
                    break;
                case PlayerState.Invincible:
                    _sprite = _spriteMove;
                    _invincibleRemaining = InvincibleDuration;
                    _turnInterval = TurnIntervalGround;
                    break;
                case PlayerState.Jumping:
                    _sprite = _spriteJump;
                    _jumpTimeRemaining = JumpDuration;
                    _turnInterval = TurnIntervalAir;
                    _currentAscension = 0;
                    _ascensionReached = false;
                    break;
                case PlayerState.Hurt:
                    _sprite = _spriteHurt;
                    _hurtTimeRemaining = HurtDuration;
                    _angle = Angle.Down;
                    _crashSound.Play();
                    break;
                case PlayerState.Victory:
                    _sprite = _spriteVictory;
                    break;
            }
        }

        public Rectangle GetHitBox()
        {
            var location = new Point((int)_position.X + Constants.ObstaclePositionOffset, (int)_position.Y + Constants.ObstaclePositionOffset);
            var size = new Point(Constants.ObstacleTileSize, Constants.ObstacleTileSize);
            var rect = new Rectangle(location, size);

            return rect;
        }

        public void Crash()
        {
            if (_currentState == PlayerState.Moving)
            {
                ChangeState(PlayerState.Hurt);
            }
        }

        public void Win()
        {
            ChangeState(PlayerState.Victory);

            if (!IsVictorious)
            {
                IsVictorious = true;
                _winSound.Play();

                var careerProgress = CareerProgress.Get();
                careerProgress.NextLevel();
            }
        }
    }
}
