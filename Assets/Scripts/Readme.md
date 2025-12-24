Player Input: | 2 + 16 + 8 + 8 + 3 + 3 = 40
    shipForwardVel -> 0-maxSpeed 
    shipSidewaysVel -> 0-maxSpeed 

    ray0 - ray15 -> 0-rayDistance | 16 INPUT

    enemy 1-2: | 8 INPUT
        distance -> 0-radarRange
        angleSin -> -1,1
        angleCos -> -1,1
        closingSpeed -> 0, maxSpeed

    mine 1-2: | 8 INPUT
        distance -> 0-radarRange
        angleSin -> -1,1
        angleCos -> -1,1
        closingSpeed -> 0, maxSpeed
    
    maxSpeed -> 4-10
    acceleration -> 3-6
    turnSpeed -> 40-100

    targetDistance -> 0-(200 * (2 ** 0.5))
    targetAngleSin -> -1,1
    targetAngleCos -> -1,1

Enemy Input: 2 + 16 + 4 + 8 + 3 = 33
    shipForwardVel -> 0-maxSpeed 
    shipSidewaysVel -> 0-maxSpeed 

    ray0 - ray15 -> 0-rayDistance | 16 INPUT

    enemy 1: | 4 INPUT
        distance -> 0-radarRange
        angleSin -> -1,1
        angleCos -> -1,1
        closingSpeed -> 0, maxSpeed

    mine 1-2: | 8 INPUT
        distance -> 0-radarRange
        angleSin -> -1,1
        angleCos -> -1,1
        closingSpeed -> 0, maxSpeed
    
    maxSpeed -> 4-10
    acceleration -> 3-6
    turnSpeed -> 40-100

