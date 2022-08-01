using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parcel.Serialization.Tests
{

    internal struct Vector3
    {
        public Vector3(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public static Vector3 Random()
        {
            Random r = new Random();
            float x = (float)r.NextDouble() * float.MaxValue;
            float y = (float)r.NextDouble() * float.MaxValue;
            float z = (float)r.NextDouble() * float.MaxValue;
            return new Vector3(x, y, z);
        }

        public override bool Equals(object? obj)
        {
            return obj is Vector3 vector &&
                   X == vector.X &&
                   Y == vector.Y &&
                   Z == vector.Z;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }
    }

    internal struct Quaternion
    {
        public Quaternion(float x, float y, float z, float w)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.W = w;
        }

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }

        public static Quaternion Random()
        {
            Random r = new Random();
            float x = (float)r.NextDouble() * float.MaxValue;
            float y = (float)r.NextDouble() * float.MaxValue;
            float z = (float)r.NextDouble() * float.MaxValue;
            float w = (float)r.NextDouble() * float.MaxValue;
            return new Quaternion(x, y, z, w);
        }

        public override bool Equals(object? obj)
        {
            return obj is Quaternion quaternion &&
                   X == quaternion.X &&
                   Y == quaternion.Y &&
                   Z == quaternion.Z &&
                   W == quaternion.W;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z, W);
        }
    }

    /// <summary>
    /// Object that mimics a potential game object
    /// </summary>
    internal class ParcelTestObject
    {
        public ulong UUID { get; set; }
        public string UnitID { get; set; }
        
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Scale { get; set; }

        public float Health { get; set; }

        public float MaxHealth { get; set; }

        public bool Invincible { get; set; }

        public ulong[] Allies { get; set; }
        public ulong[] Enemies { get; set; }

        public ParcelTestObject()
        {

        }

        public ParcelTestObject(ulong uUID, string unitID, Vector3 position, Quaternion rotation, Vector3 scale, float health, float maxHealth, bool invincible, ulong[] allies, ulong[] enemies)
        {
            UUID = uUID;
            UnitID = unitID;
            Position = position;
            Rotation = rotation;
            Scale = scale;
            Health = health;
            MaxHealth = maxHealth;
            Invincible = invincible;
            Allies = allies;
            Enemies = enemies;
        }

        public static ParcelTestObject Random()
        {
            Random r = new Random();
            ulong uuid = (ulong)r.Next(0, int.MaxValue);

            char[] chars = new char[r.Next(10, 45)];
            for (int i = 0; i < chars.Length; i++)
                chars[i] = (char)r.Next(65, 91);
            string unitID = new string(chars);

            Vector3 position = Vector3.Random();
            Quaternion rotation = Quaternion.Random();
            Vector3 scale = Vector3.Random();

            float health = (float)r.NextDouble() * float.MaxValue;
            float maxHealth = (float)r.NextDouble() * float.MaxValue;
            bool invincible = r.Next(0, 2) == 1;

            ulong[] allies = new ulong[r.Next(10, 20)];
            for (int i = 0; i < allies.Length; i++)
                allies[i] = (ulong)r.Next(0, int.MaxValue);

            ulong[] enemies = new ulong[r.Next(10, 20)];
            for (int i = 0; i < enemies.Length; i++)
                enemies[i] = (ulong)r.Next(0, int.MaxValue);

            return new ParcelTestObject(uuid, unitID, position, rotation, scale, health, maxHealth, invincible, allies, enemies);
        }

        public override bool Equals(object? obj)
        {
            if (obj is ParcelTestObject @object)
            {
                Console.WriteLine($"{nameof(UUID)} | this: {UUID} that: {@object.UUID} equal?: {UUID == @object.UUID}");
                Console.WriteLine($"{nameof(UnitID)} | this: {UnitID} that: {@object.UnitID} equal?: {UnitID == @object.UnitID}");
                Console.WriteLine($"{nameof(Position)}Position | this: {Position} that: {@object.Position} equal?: {Position.Equals(@object.Position)}");
                Console.WriteLine($"{nameof(Rotation)} | this: {Rotation} that: {@object.Rotation} equal?: {Rotation.Equals(@object.Rotation)}");
                Console.WriteLine($"{nameof(Scale)} | this: {Scale} that: {@object.Scale} equal?: {Scale.Equals(@object.Scale)}");
                Console.WriteLine($"{nameof(Health)} | this: {Health} that: {@object.Health} equal?: {Health == @object.Health}");
                Console.WriteLine($"{nameof(MaxHealth)} | this: {MaxHealth} that: {@object.MaxHealth} equal?: {MaxHealth == @object.MaxHealth}");
                Console.WriteLine($"{nameof(Invincible)} | this: {Invincible} that: {@object.Invincible} equal?: {Invincible == @object.Invincible}");

                for (int i = 0; i < Allies.Length; i++)
                    Console.WriteLine($"{nameof(Allies)}[{i}] | this: {Allies[i]} that: {@object.Allies[i]} equal?: {Allies[i] == @object.Allies[i]}");

                for (int i = 0; i < Enemies.Length; i++)
                    Console.WriteLine($"{nameof(Enemies)}[{i}] | this: {Enemies[i]} that: {@object.Enemies[i]} equal?: {Enemies[i] == @object.Enemies[i]}");

                return UUID == @object.UUID &&
                   UnitID == @object.UnitID &&
                   Position.Equals(@object.Position) &&
                   Rotation.Equals(@object.Rotation) &&
                   Scale.Equals(@object.Scale) &&
                   Health == @object.Health &&
                   MaxHealth == @object.MaxHealth &&
                   Invincible == @object.Invincible &&
                   Enumerable.SequenceEqual(Allies, @object.Allies) &&
                   Enumerable.SequenceEqual(Enemies, @object.Enemies);
            }
            return false;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(UUID);
            hash.Add(UnitID);
            hash.Add(Position);
            hash.Add(Rotation);
            hash.Add(Scale);
            hash.Add(Health);
            hash.Add(MaxHealth);
            hash.Add(Invincible);
            hash.Add(Allies);
            hash.Add(Enemies);
            return hash.ToHashCode();
        }
    }

    internal class ParcelTestChild : ParcelTestObject
    {
        public string MyChild { get; set; }

        public ParcelTestChild()
        {

        }

        public ParcelTestChild(ulong uUID, string unitID, Vector3 position, Quaternion rotation, Vector3 scale, float health, float maxHealth, bool invincible, ulong[] allies, ulong[] enemies, string myChild)
        {
            UUID = uUID;
            UnitID = unitID;
            Position = position;
            Rotation = rotation;
            Scale = scale;
            Health = health;
            MaxHealth = maxHealth;
            Invincible = invincible;
            Allies = allies;
            Enemies = enemies;
            MyChild = myChild;
        }

        public static ParcelTestChild Random()
        {
            Random r = new Random();
            ulong uuid = (ulong)r.Next(0, int.MaxValue);

            char[] chars = new char[r.Next(10, 45)];
            for (int i = 0; i < chars.Length; i++)
                chars[i] = (char)r.Next(65, 91);
            string unitID = new string(chars);

            Vector3 position = Vector3.Random();
            Quaternion rotation = Quaternion.Random();
            Vector3 scale = Vector3.Random();

            float health = (float)r.NextDouble() * float.MaxValue;
            float maxHealth = (float)r.NextDouble() * float.MaxValue;
            bool invincible = r.Next(0, 2) == 1;

            ulong[] allies = new ulong[r.Next(10, 20)];
            for (int i = 0; i < allies.Length; i++)
                allies[i] = (ulong)r.Next(0, int.MaxValue);

            ulong[] enemies = new ulong[r.Next(10, 20)];
            for (int i = 0; i < enemies.Length; i++)
                enemies[i] = (ulong)r.Next(0, int.MaxValue);

            chars = new char[r.Next(10, 45)];
            for (int i = 0; i < chars.Length; i++)
                chars[i] = (char)r.Next(65, 91);
            string myChild = new string(chars);

            return new ParcelTestChild(uuid, unitID, position, rotation, scale, health, maxHealth, invincible, allies, enemies, myChild);
        }

        public override bool Equals(object? obj)
        {
            if (obj is ParcelTestChild @object)
            {
                Console.WriteLine($"{nameof(UUID)} | this: {UUID} that: {@object.UUID} equal?: {UUID == @object.UUID}");
                Console.WriteLine($"{nameof(UnitID)} | this: {UnitID} that: {@object.UnitID} equal?: {UnitID == @object.UnitID}");
                Console.WriteLine($"{nameof(Position)}Position | this: {Position} that: {@object.Position} equal?: {Position.Equals(@object.Position)}");
                Console.WriteLine($"{nameof(Rotation)} | this: {Rotation} that: {@object.Rotation} equal?: {Rotation.Equals(@object.Rotation)}");
                Console.WriteLine($"{nameof(Scale)} | this: {Scale} that: {@object.Scale} equal?: {Scale.Equals(@object.Scale)}");
                Console.WriteLine($"{nameof(Health)} | this: {Health} that: {@object.Health} equal?: {Health == @object.Health}");
                Console.WriteLine($"{nameof(MaxHealth)} | this: {MaxHealth} that: {@object.MaxHealth} equal?: {MaxHealth == @object.MaxHealth}");
                Console.WriteLine($"{nameof(Invincible)} | this: {Invincible} that: {@object.Invincible} equal?: {Invincible == @object.Invincible}");

                for (int i = 0; i < Allies.Length; i++)
                    Console.WriteLine($"{nameof(Allies)}[{i}] | this: {Allies[i]} that: {@object.Allies[i]} equal?: {Allies[i] == @object.Allies[i]}");

                for (int i = 0; i < Enemies.Length; i++)
                    Console.WriteLine($"{nameof(Enemies)}[{i}] | this: {Enemies[i]} that: {@object.Enemies[i]} equal?: {Enemies[i] == @object.Enemies[i]}");
                
                Console.WriteLine($"{nameof(MyChild)} | this: {MyChild} that: {@object.MyChild} equal?: {MyChild == @object.MyChild}");

                return UUID == @object.UUID &&
                   UnitID == @object.UnitID &&
                   Position.Equals(@object.Position) &&
                   Rotation.Equals(@object.Rotation) &&
                   Scale.Equals(@object.Scale) &&
                   Health == @object.Health &&
                   MaxHealth == @object.MaxHealth &&
                   Invincible == @object.Invincible &&
                   Enumerable.SequenceEqual(Allies, @object.Allies) &&
                   Enumerable.SequenceEqual(Enemies, @object.Enemies) &&
                   MyChild == @object.MyChild;
            }
            return false;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(UUID);
            hash.Add(UnitID);
            hash.Add(Position);
            hash.Add(Rotation);
            hash.Add(Scale);
            hash.Add(Health);
            hash.Add(MaxHealth);
            hash.Add(Invincible);
            hash.Add(Allies);
            hash.Add(Enemies);
            hash.Add(MyChild);
            return hash.ToHashCode();
        }
    }
}
