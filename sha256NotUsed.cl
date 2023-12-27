//Debug default
#ifdef USE_DEBUG_DEFAULTS

    #define KEY_LENGTH 17
    #define SALT_LENGTH 6
    #define SALT_STRING Hf45DD

    //Key: 'enclosesHf45DD' (8:6)
    #define HASH_0 2627633883
    #define HASH_1 342531070
    #define HASH_2 1941191403
    #define HASH_3 4203335048
    #define HASH_4 2588921030
    #define HASH_5 4270942822
    #define HASH_6 3704920044
    #define HASH_7 3301016226

#endif


//Base SHA-256 context
#define H0 0x6a09e667
#define H1 0xbb67ae85
#define H2 0x3c6ef372
#define H3 0xa54ff53a
#define H4 0x510e527f
#define H5 0x9b05688c
#define H6 0x1f83d9ab
#define H7 0x5be0cd19

//String convert macro
#define STR(s) #s
#define XSTR(s) STR(s)

//Methods
// << : bitshift left
// >> : bitshift right
// ^  : bitwise XOR
// ~  : bitwise NOT
// &  : bitwise AND
// |  : bitwise OR


inline uint rotr(uint x, int n) //Rotate right
{
    return (x >> n) | (x << (32 - n));
}
inline uint ch(uint x, uint y, uint z) //Choice based on x
{
    return (x & y) ^ (~x & z);
}
inline uint maj(uint x, uint y, uint z) //Majority of bits in x, y
{
    return (x & y) ^ (x & z) ^ (y & z);
}
inline uint sig0(uint x)
{
    return rotr(x, 7) ^ rotr(x, 18) ^ (x >> 3);
}
inline uint sig1(uint x)
{
    return rotr(x, 17) ^ rotr(x, 19) ^ (x >> 10);
}
inline uint csig0(uint x)
{
    return rotr(x, 2) ^ rotr(x, 13) ^ rotr(x, 22);
}
inline uint csig1(uint x)
{
    return rotr(x, 6) ^ rotr(x, 11) ^ rotr(x, 25);
}

//crack_single_kernel.cl

kernel void sha256crack_single_kernel(global uchar* keys, global uint* result)
{
    //Initialize
    int qua; //Message schedule step modulus
    int mod; //Message schedule step modulus
    uint length; //Message schedule    
    uint A, B, C, D, E, F, G, H; //Compression targets
    uint T1, T2; //Compression temp
    uint globalID; //Global worker id
    global uchar* key; //Global target key location
    uint W[80];
    const uint K[64] =
    {
        0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5, 0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
        0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3, 0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
        0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc, 0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
        0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7, 0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
        0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13, 0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
        0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3, 0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
        0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5, 0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
        0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208, 0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2
    };


    //Get key
    globalID = get_global_id(0);
    key = keys + globalID * KEY_LENGTH; //Get pointer to key string

    for (length = 0; length < KEY_LENGTH && (key[length] != 0 && key[length] != '\n'); length++) { }
    key[length] = 0;

    //Reset algorithm
    #pragma unroll
    for (int i = 0; i < 80; i++)
    {
        W[i] = 0x00000000;
    }


    //Create message block
    qua = length / 4;
    mod = length % 4;
    for (int i = 0; i < qua; i++)
    {
        W[i]  = (key[i * 4 + 0]) << 24;
        W[i] |= (key[i * 4 + 1]) << 16;
        W[i] |= (key[i * 4 + 2]) << 8;
        W[i] |= (key[i * 4 + 3]);
    }

    //Pad remaining uint
    if (mod == 0)
    {
        W[qua] = 0x80000000;
    }
    else if (mod == 1)
    {
        W[qua] = (key[qua * 4]) << 24;
        W[qua] |= 0x800000;
    }
    else if (mod == 2)
    {
        W[qua] = (key[qua * 4]) << 24;
        W[qua] |= (key[qua * 4 + 1]) << 16;
        W[qua] |= 0x8000;
    }
    else
    {
        W[qua] = (key[qua * 4]) << 24;
        W[qua] |= (key[qua * 4 + 1]) << 16;
        W[qua] |= (key[qua * 4 + 2]) << 8;
        W[qua] |= 0x80;
    }

    W[15] = length * 8; //Add key length


    //Run message schedule
    #pragma unroll
    for (int i = 16; i < 64; i++)
    {
        W[i] = sig1(W[i - 2]) + W[i - 7] + sig0(W[i - 15]) + W[i - 16];
    }


    //Prepare compression
    A = H0;
    B = H1;
    C = H2;
    D = H3;
    E = H4;
    F = H5;
    G = H6;
    H = H7;


    //Compress
    #pragma unroll
    for (int i = 0; i < 64; i++)
    {
        //Compress temporary
        T1 = H + csig1(E) + ch(E, F, G) + K[i] + W[i];
        T2 = csig0(A) + maj(A, B, C);

        //Rotate over, override H
        H = G;
        G = F;
        F = E;
        E = D + T1;
        D = C;
        C = B;
        B = A;
        A = T1 + T2;
    }
    

    //Verify result
    if (A + H0 == HASH_0 && B + H1 == HASH_1 &&
        C + H2 == HASH_2 && D + H3 == HASH_3 &&
        E + H4 == HASH_4 && F + H5 == HASH_5 &&
        G + H6 == HASH_6 && H + H7 == HASH_7)
    {
        *result = globalID + 1;
    }
}

// crack_single_salted_kernel

kernel void sha256crack_single_salted_kernel(global char* keys, global uint* result)
{

    uint h0 = H0;
    uint h1 = H1;
    uint h2 = H2;
    uint h3 = H3;
    uint h4 = H4;
    uint h5 = H5;
    uint h6 = H6;
    uint h7 = H7;
    //Initialize
    int qua; //Message schedule step modulus
    int mod; //Message schedule step modulus
    uint length; //Message schedule    
    uint A, B, C, D, E, F, G, H; //Compression targets
    uint T1, T2; //Compression temp
    uint globalID; //Global worker id
    global uchar* globalKey; //Global target key location
    char key[KEY_LENGTH + SALT_LENGTH + 1]; //Cache key storage
    uint W[80];
    const uint K[64] =
    {
        0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5, 0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
        0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3, 0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
        0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc, 0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
        0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7, 0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
        0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13, 0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
        0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3, 0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
        0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5, 0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
        0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208, 0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2
    };


    //Get key
    globalID = get_global_id(0);
    globalKey = keys + globalID * KEY_LENGTH; //Get pointer to key string
    for (length = 0; length < KEY_LENGTH && (globalKey[length] != 0 && globalKey[length] != '\n'); length++)
    {
        key[length] = globalKey[length];
    }

    
    //Append salt
    #pragma unroll
    for (uint i = 0; i < SALT_LENGTH; i++)
    {
        key[length + i] = XSTR(SALT_STRING)[i];
    }
    length += SALT_LENGTH;
    key[length] = 0;


    //Reset algorithm
    #pragma unroll
    for (uint i = 0; i < 80; i++)
    {
        W[i] = 0x00000000;
    }


    //Create message block
    qua = length / 4;
    mod = length % 4;
    for (uint i = 0; i < qua; i++)
    {
        W[i]  = (key[i * 4 + 0]) << 24;
        W[i] |= (key[i * 4 + 1]) << 16;
        W[i] |= (key[i * 4 + 2]) << 8;
        W[i] |= (key[i * 4 + 3]);
    }

    //Pad remaining uint
    switch (mod)
    {
        //l = n * 4 + 0
        case 0:
            W[qua] = 0x80000000;
            break;

        //l = n * 4 + 3
        case 3:
            W[qua]  = (key[qua * 4 + 0]) << 24;
            W[qua] |= (key[qua * 4 + 1]) << 16;
            W[qua] |= (key[qua * 4 + 2]) << 8;
            W[qua] |= 0x80;
            break;

        //l = n * 4 + 2
        case 2:
            W[qua]  = (key[qua * 4 + 0]) << 24;
            W[qua] |= (key[qua * 4 + 1]) << 16;
            W[qua] |= 0x8000;
            break;

        //l = n * 4 + 1
        case 1:
            W[qua]  = (key[qua * 4 + 0]) << 24;
            W[qua] |= 0x800000;
            break;
    }

    W[15] = length * 8; //Add key length


    //Run message schedule
    #pragma unroll
    for (uint i = 16; i < 64; i++)
    {
        W[i] = sig1(W[i - 2]) + W[i - 7] + sig0(W[i - 15]) + W[i - 16];
    }


    //Prepare compression
    A = H0;
    B = H1;
    C = H2;
    D = H3;
    E = H4;
    F = H5;
    G = H6;
    H = H7;


    //Compress
    #pragma unroll
    for (uint i = 0; i < 64; i++)
    {
        //Compress temporary
        T1 = H + csig1(E) + ch(E, F, G) + K[i] + W[i];
        T2 = csig0(A) + maj(A, B, C);

        //Rotate over, override H
        H = G;
        G = F;
        F = E;
        E = D + T1;
        D = C;
        C = B;
        B = A;
        A = T1 + T2;
    }


    //Verify result
    if (A == HASH_0 - H0 && B == HASH_1 - H1 &&
        C == HASH_2 - H2 && D == HASH_3 - H3 &&
        E == HASH_4 - H4 && F == HASH_5 - H5 &&
        G == HASH_6 - H6 && H == HASH_7 - H7)
    {
        *result = globalID + 1;
    }
}

// hash_multiple_kernel

kernel void sha256hash_multiple_kernel(uint keyLength, global uchar* keys, global char* result)
{
    //Initialize
    int qua; //Message schedule step modulus
    int mod; //Message schedule step modulus
    uint length; //Message schedule    
    uint A, B, C, D, E, F, G, H; //Compression targets
    uint T1, T2; //Compression temp
    uint globalID; //Global worker id
    global uchar* key; //Global target key location
    uint W[80];
    const uint K[64] =
    {
       0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5, 0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
       0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3, 0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
       0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc, 0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
       0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7, 0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
       0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13, 0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
       0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3, 0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
       0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5, 0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
       0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208, 0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2
    };


    //Get key
    globalID = get_global_id(0);
    key = keys + globalID * keyLength; //Get pointer to key string

    for (length = 0; length < keyLength && (key[length] != 0 && key[length] != '\n'); length++) { }
    key[length] = 0;


    //Reset algorithm
    #pragma unroll
    for (int i = 0; i < 80; i++)
    {
        W[i] = 0x00000000;
    }


    //Create message block
    qua = length / 4;
    mod = length % 4;
    for (int i = 0; i < qua; i++)
    {
        W[i]  = (key[i * 4 + 0]) << 24;
        W[i] |= (key[i * 4 + 1]) << 16;
        W[i] |= (key[i * 4 + 2]) << 8;
        W[i] |= (key[i * 4 + 3]);
    }

    //Pad remaining uint
    if (mod == 0)
    {
        W[qua] = 0x80000000;
    }
    else if (mod == 1)
    {
        W[qua] = (key[qua * 4]) << 24;
        W[qua] |= 0x800000;
    }
    else if (mod == 2)
    {
        W[qua] = (key[qua * 4]) << 24;
        W[qua] |= (key[qua * 4 + 1]) << 16;
        W[qua] |= 0x8000;
    }
    else
    {
        W[qua] = (key[qua * 4]) << 24;
        W[qua] |= (key[qua * 4 + 1]) << 16;
        W[qua] |= (key[qua * 4 + 2]) << 8;
        W[qua] |= 0x80;
    }

    W[15] = length * 8; //Add key length


    //Run message schedule
    #pragma unroll
    for (int i = 16; i < 64; i++)
    {
        W[i] = sig1(W[i - 2]) + W[i - 7] + sig0(W[i - 15]) + W[i - 16];
    }


    //Prepare compression
    A = H0;
    B = H1;
    C = H2;
    D = H3;
    E = H4;
    F = H5;
    G = H6;
    H = H7;


    //Compress
    #pragma unroll
    for (int i = 0; i < 64; i++)
    {
        //Compress temporary
        T1 = H + csig1(E) + ch(E, F, G) + K[i] + W[i];
        T2 = csig0(A) + maj(A, B, C);

        //Rotate over, override H
        H = G;
        G = F;
        F = E;
        E = D + T1;
        D = C;
        C = B;
        B = A;
        A = T1 + T2;
    }

    W[0] = A + H0;
    W[1] = B + H1;
    W[2] = C + H2;
    W[3] = D + H3;
    W[4] = E + H4;
    W[5] = F + H5;
    W[6] = G + H6;
    W[7] = H + H7;

    
    //Convert uints to hex char array
    char hex_charset[] = "0123456789abcdef";
    #pragma unroll
    for (int j = 0; j < 8; j++)
    {
        #pragma unroll
        for (int len = 8-1; len >= 0; W[j] >>= 4, --len)
        {
            result[(globalID * 65) + (j * 8) + len] = hex_charset[ W[j] & 0xf ];
        }
    }
    result[(globalID * 65) + 64] = '\n';
    //printf("%s %u\n", &result[(globalID * 65)], globalID);
}

// hash_single_kernel

kernel void sha256single_kernel(uint keyLength, global char* key, global char* result)
{
    //Initialize
    int qua; //Message schedule step quantity
    int mod; //Message schedule step modulus
    uint A, B, C, D, E, F, G, H; //Compression targets
    uint T1, T2; //Compression temp
    uint globalID; //Global worker id
    uint W[80]; //Message schedule
    const uint K[64] = //Cube roots of first 64 primes
    {
       0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5, 0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
       0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3, 0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
       0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc, 0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
       0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7, 0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
       0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13, 0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
       0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3, 0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
       0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5, 0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
       0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208, 0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2
    };


    //Reset algorithm
    #pragma unroll
    for (int i = 0; i < 80; i++)
    {
        W[i] = 0x00000000;
    }


    //Create message block
    qua = keyLength / 4;
    mod = keyLength % 4;
    for (int i = 0; i < qua; i++)
    {
        W[i]  = (key[i * 4 + 0]) << 24;
        W[i] |= (key[i * 4 + 1]) << 16;
        W[i] |= (key[i * 4 + 2]) << 8;
        W[i] |= (key[i * 4 + 3]);
    }

    //Pad remaining uint
    if (mod == 0)
    {
        W[qua] = 0x80000000;
    }
    else if (mod == 1)
    {
        W[qua] = (key[qua * 4]) << 24;
        W[qua] |= 0x800000;
    }
    else if (mod == 2)
    {
        W[qua] = (key[qua * 4]) << 24;
        W[qua] |= (key[qua * 4 + 1]) << 16;
        W[qua] |= 0x8000;
    }
    else
    {
        W[qua] = (key[qua * 4]) << 24;
        W[qua] |= (key[qua * 4 + 1]) << 16;
        W[qua] |= (key[qua * 4 + 2]) << 8;
        W[qua] |= 0x80;
    }

    W[15] = keyLength * 8; //Add key length


    //Run message schedule
    #pragma unroll
    for (int i = 16; i < 64; i++)
    {
        W[i] = sig1(W[i - 2]) + W[i - 7] + sig0(W[i - 15]) + W[i - 16];
    }


    //Prepare compression
    A = H0;
    B = H1;
    C = H2;
    D = H3;
    E = H4;
    F = H5;
    G = H6;
    H = H7;


    //Compress
    #pragma unroll
    for (int i = 0; i < 64; i++)
    {
        //Compress temporary
        T1 = H + csig1(E) + ch(E, F, G) + K[i] + W[i];
        T2 = csig0(A) + maj(A, B, C);

        //Rotate over, override H
        H = G;
        G = F;
        F = E;
        E = D + T1;
        D = C;
        C = B;
        B = A;
        A = T1 + T2;
    }

    W[0] = A + H0;
    W[1] = B + H1;
    W[2] = C + H2;
    W[3] = D + H3;
    W[4] = E + H4;
    W[5] = F + H5;
    W[6] = G + H6;
    W[7] = H + H7;


    //Convert uints to hex char array
    char hex_charset[] = "0123456789abcdef";
    #pragma unroll
    for (int j = 0; j < 8; j++)
    {
        #pragma unroll
        for (int len = 8-1; len >= 0; W[j] >>= 4, --len)
        {
            result[(j * 8) + len] = hex_charset[ W[j] & 0xf ];
        }
    }
    result[64] = 0;

    //printf("%s\n", result);
}
