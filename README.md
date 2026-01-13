# 2D Naval Warfare - Enemy Ship AI

This repository is an attempt to create enemy ship agent in 2d naval game. The agent's goal is to prevent player reaching the target point by colliding with it or throwing a mine.

### Training Pipeline

Since we have walls in our game, we need to teach agents to navigate first. For that I used curriculum learning with some dense reward shaping. Also, in order to train the enemy, we first need decent player. So our initial goal is the train the player.

Below are the quick recap. See config files for curriculums.


#### Phase 0

We need decent player first.

- I freeze the enemies - but still can fire mines (shouldn't be matter tho since enemies outputs are just random).
- Dense shaping based on how much get closer to the target compare to last step
- Dense shaping based on how close to the closest mine - accounting time to explotion  
- Dense shaping based on how close to the closest enemy
- On death -1
- Reaching target +5 
- On timeout, based on progress
- On enemy death +0.3 (not sure about that)
- -0.0005f as step penalty

Assign the dummy enemy model to enemy prefab and make behaviour type inference only. Set player behaviour type to default and get a build. Then inside `pythonEnv`

```
mlagents-learn config/playerOnly.yaml --env=yourpath.x86_64 --run-id=playerShip_v1 --time-scale=40
```

In 10M steps, here is the result:

<img src="./media/playerv1.png">

It can consistently reach the target.

#### Phase 1 

Now we can train enemy ship.

Note: I tried to use radar for enemies and not giving the target information because I thought that would make those cheat but without that enemies couldn't learn.

- Dense rewards based on phases. At the end, we basically give reward for being between player and target.
- -0.5 on death
- +1.5 on player death by hittin walls
- +5 on player death by hitting mines
- +5 for colliding with player
- -10 for player reaches the target

Set playerShip_v1 to player and set behaviour type to inference. Set enemyShip behaviour type to default and get a build.

```
 mlagents-learn config/enemyOnly.yaml --env=Builds/yourpath/game.x86_64 --run-id=enemyShip_v1 --time-scale=40
```

<img src="./media/enemyv1.png">



