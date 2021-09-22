using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

/**
 * Custom implementation of an hash function named xxHash by Yann Collet
 * http://xxhash.com/
 */
public readonly struct SmallXXHash {

    const uint primeA = 0b10011110001101110111100110110001;
    const uint primeB = 0b10000101111010111100101001110111;
    const uint primeC = 0b11000010101100101010111000111101;
    const uint primeD = 0b00100111110101001110101100101111;
    const uint primeE = 0b00010110010101100110011110110001;

    private readonly uint accumulator;

    public static implicit operator uint(SmallXXHash hash)
    {
        uint avalanche = hash.accumulator;
        avalanche ^= avalanche >> 15;
        avalanche *= primeB;
        avalanche ^= avalanche >> 13;
        avalanche *= primeC;
        avalanche ^= avalanche >> 16;
        return avalanche;
    }
    
    public static implicit operator SmallXXHash4 (SmallXXHash hash) =>
        new SmallXXHash4(hash.accumulator);
    
    public SmallXXHash(uint accumulator)
    {
        this.accumulator = accumulator;
    }

    public static SmallXXHash Seed(int seed) => (uint) seed + primeE;
    public static implicit operator SmallXXHash(uint accumulator) => new SmallXXHash(accumulator);

    public SmallXXHash Eat(byte data) => RotateLeft(accumulator + data * primeE, 11) * primeA;
    public SmallXXHash Eat(int data) => RotateLeft(accumulator + (uint) data * primeC, 17) * primeD;

    static uint RotateLeft(uint data, int steps) => (data << steps) | (data >> 32 - steps); 
}

public readonly struct SmallXXHash4 {

    const uint primeB = 0b10000101111010111100101001110111;
    const uint primeC = 0b11000010101100101010111000111101;
    const uint primeD = 0b00100111110101001110101100101111;
    const uint primeE = 0b00010110010101100110011110110001;

    private readonly uint4 accumulator;

    public static implicit operator uint4(SmallXXHash4 hash)
    {
        uint4 avalanche = hash.accumulator;
        avalanche ^= avalanche >> 15;
        avalanche *= primeB;
        avalanche ^= avalanche >> 13;
        avalanche *= primeC;
        avalanche ^= avalanche >> 16;
        return avalanche;
    }
    
    public SmallXXHash4(uint4 accumulator)
    {
        this.accumulator = accumulator;
    }

    public static SmallXXHash4 Seed(int4 seed) => (uint4) seed + primeE;
    public static implicit operator SmallXXHash4(uint4 accumulator) => new SmallXXHash4(accumulator);

    public SmallXXHash4 Eat(int4 data) => RotateLeft(accumulator + (uint4) data * primeC, 17) * primeD;

    static uint4 RotateLeft(uint4 data, int steps) => (data << steps) | (data >> 32 - steps); 
}