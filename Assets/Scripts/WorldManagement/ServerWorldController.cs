﻿using System;
using System.Collections.Generic;
using System.Linq;
using Connections.Loggers;
using Connections.Streams;
using DefaultNamespace;
using UnityEngine;
using Random = System.Random;

namespace WorldManagement
{
    public class ServerWorldController : WorldController
    {
        private byte _lastEnemyId = 0;
        private byte _lastCharacterId = 0;
        private int _xRange;
        private int _zRange;
        private int _spawnRate;
        private Random _random = new Random();
        private Material _enemiesMaterial;
        private Dictionary<byte, EnemyController> enemyControllers = new Dictionary<byte, EnemyController>();
        private int _spawnRateTick = 0;

        public ServerWorldController(int spawnRate, ServerLogger logger) : base(logger)
        {
            Vector3 planeScale = GameObject.FindWithTag("Platform").transform.localScale*5;
            _xRange = (int)(planeScale.x * 2);
            _zRange = (int)(planeScale.z * 2);
            _spawnRate = spawnRate;
        }

        public byte SpawnCharacter()
        {
            SpawnCharacter(_lastCharacterId, Color.white, true);
            base.ObjectsToCreate.Enqueue(new Tuple<byte, PrimitiveType>(_lastCharacterId, PrimitiveType.Capsule));
            return _lastCharacterId++;
        }

        public byte GetMovementSpeed()
        {
            return _movementSpeed;
        }

        public void MoveObject(byte id, Vector3 movement, bool isCharacter)
        {
            Dictionary<byte, GameObject> objectDict = isCharacter ? _characters : _enemies;
            objectDict[id].GetComponent<CharacterController>().Move(movement);
        }

        public void SpawnEnemy()
        {
            Vector3 pos = new Vector3(_random.Next(_xRange) - _xRange/2 , 1.1f, _random.Next(_zRange) - _zRange/2);
            GameObject enemy = SpawnObject(_lastEnemyId, PrimitiveType.Cylinder, pos, Color.cyan, true);
            enemyControllers[_lastEnemyId] = new EnemyController(this, enemy, _lastEnemyId);
            base.ObjectsToCreate.Enqueue(new Tuple<byte, PrimitiveType>(_lastEnemyId, PrimitiveType.Cylinder));
            _lastEnemyId++;
        }

        public byte[] GetPositions(byte id)
        {
            return base.GetPositions(id);
        }

        public void Update()
        {
            foreach (KeyValuePair<byte, EnemyController> pair in enemyControllers)
            {
                pair.Value.Update();
            }

//            if (++_spawnRateTick == _spawnRate)
//            {
//                SpawnEnemy();
//                _spawnRateTick = 0;
//            }
        }

        public void PlayerAttacked(byte playerId)
        {
            HashSet<byte> enemiesToDelete = AttackNPCsNearPoint(_characters[playerId].transform.position);
            foreach (byte id in enemiesToDelete)
            {
                enemyControllers.Remove(id);
                base.ObjectsToDestroy.Enqueue(new Tuple<byte, PrimitiveType>(id, PrimitiveType.Cylinder));
            }
        }

        public void MoveEnemy(byte id, Vector3 move, byte playerId)
        {
            if (!_characters.ContainsKey(playerId))
            {
                return;
            }
            Vector3 playerPos = _characters[playerId].transform.position;
            MoveObject(id, move, false);
            if (_enemies[id].transform.position.Equals(playerPos))
            {
                AttackPlayer(playerId);
            }
        }

        private void AttackPlayer(byte playerId)
        {
            DestroyGameObject(playerId, true);
            base.ObjectsToDestroy.Enqueue(new Tuple<byte, PrimitiveType>(playerId, PrimitiveType.Capsule));
        }

        public Queue<Tuple<byte, PrimitiveType>> ObjectsToCreate()
        {
            Queue<Tuple<byte, PrimitiveType>> toReturn = base.ObjectsToCreate;
            base.ObjectsToCreate = new Queue<Tuple<byte, PrimitiveType>>();
            return toReturn;
        }

        public Dictionary<byte, GameObject> GetCharacters()
        {
            return _characters;
        }

        public void MoveCharacter(byte id, InputPackage inputPackage)
        {
            MoveObject(id, InputUtils.DecodeInput(inputPackage.input), true);
            characterLastInputIds[id] = inputPackage.id;
        }

        public void DestroyObject(byte enemyId, bool isChar)
        {
            DestroyGameObject(enemyId, isChar);
        }
    }
}