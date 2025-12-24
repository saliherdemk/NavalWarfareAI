mlagents-learn config/template.yaml --env=Build/NavalEnv.x86_64 --run-id=ship_v1
 --resume --no-graphics --time-scale=20

```
PlayerSolo/
    week1 -> Set Enemy Behavior Type to Heuristic
    week2 -> Use week1 model for Enemy Inference

EnemySolo/
    week1 -> Set Player Behavior Type to Heuristic 
    week2 -> Use week2 model for Player Inference
```

