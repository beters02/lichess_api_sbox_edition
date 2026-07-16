#nullable enable annotations

using System.Text;

namespace LichessNET.OAuth;

internal static class ManagedCrypto
{
    private static readonly uint[] ShaK =
    {
        0x428a2f98,0x71374491,0xb5c0fbcf,0xe9b5dba5,0x3956c25b,0x59f111f1,0x923f82a4,0xab1c5ed5,
        0xd807aa98,0x12835b01,0x243185be,0x550c7dc3,0x72be5d74,0x80deb1fe,0x9bdc06a7,0xc19bf174,
        0xe49b69c1,0xefbe4786,0x0fc19dc6,0x240ca1cc,0x2de92c6f,0x4a7484aa,0x5cb0a9dc,0x76f988da,
        0x983e5152,0xa831c66d,0xb00327c8,0xbf597fc7,0xc6e00bf3,0xd5a79147,0x06ca6351,0x14292967,
        0x27b70a85,0x2e1b2138,0x4d2c6dfc,0x53380d13,0x650a7354,0x766a0abb,0x81c2c92e,0x92722c85,
        0xa2bfe8a1,0xa81a664b,0xc24b8b70,0xc76c51a3,0xd192e819,0xd6990624,0xf40e3585,0x106aa070,
        0x19a4c116,0x1e376c08,0x2748774c,0x34b0bcb5,0x391c0cb3,0x4ed8aa4a,0x5b9cca4f,0x682e6ff3,
        0x748f82ee,0x78a5636f,0x84c87814,0x8cc70208,0x90befffa,0xa4506ceb,0xbef9a3f7,0xc67178f2
    };

    private static readonly byte[] SBox =
    {
        0x63,0x7c,0x77,0x7b,0xf2,0x6b,0x6f,0xc5,0x30,0x01,0x67,0x2b,0xfe,0xd7,0xab,0x76,
        0xca,0x82,0xc9,0x7d,0xfa,0x59,0x47,0xf0,0xad,0xd4,0xa2,0xaf,0x9c,0xa4,0x72,0xc0,
        0xb7,0xfd,0x93,0x26,0x36,0x3f,0xf7,0xcc,0x34,0xa5,0xe5,0xf1,0x71,0xd8,0x31,0x15,
        0x04,0xc7,0x23,0xc3,0x18,0x96,0x05,0x9a,0x07,0x12,0x80,0xe2,0xeb,0x27,0xb2,0x75,
        0x09,0x83,0x2c,0x1a,0x1b,0x6e,0x5a,0xa0,0x52,0x3b,0xd6,0xb3,0x29,0xe3,0x2f,0x84,
        0x53,0xd1,0x00,0xed,0x20,0xfc,0xb1,0x5b,0x6a,0xcb,0xbe,0x39,0x4a,0x4c,0x58,0xcf,
        0xd0,0xef,0xaa,0xfb,0x43,0x4d,0x33,0x85,0x45,0xf9,0x02,0x7f,0x50,0x3c,0x9f,0xa8,
        0x51,0xa3,0x40,0x8f,0x92,0x9d,0x38,0xf5,0xbc,0xb6,0xda,0x21,0x10,0xff,0xf3,0xd2,
        0xcd,0x0c,0x13,0xec,0x5f,0x97,0x44,0x17,0xc4,0xa7,0x7e,0x3d,0x64,0x5d,0x19,0x73,
        0x60,0x81,0x4f,0xdc,0x22,0x2a,0x90,0x88,0x46,0xee,0xb8,0x14,0xde,0x5e,0x0b,0xdb,
        0xe0,0x32,0x3a,0x0a,0x49,0x06,0x24,0x5c,0xc2,0xd3,0xac,0x62,0x91,0x95,0xe4,0x79,
        0xe7,0xc8,0x37,0x6d,0x8d,0xd5,0x4e,0xa9,0x6c,0x56,0xf4,0xea,0x65,0x7a,0xae,0x08,
        0xba,0x78,0x25,0x2e,0x1c,0xa6,0xb4,0xc6,0xe8,0xdd,0x74,0x1f,0x4b,0xbd,0x8b,0x8a,
        0x70,0x3e,0xb5,0x66,0x48,0x03,0xf6,0x0e,0x61,0x35,0x57,0xb9,0x86,0xc1,0x1d,0x9e,
        0xe1,0xf8,0x98,0x11,0x69,0xd9,0x8e,0x94,0x9b,0x1e,0x87,0xe9,0xce,0x55,0x28,0xdf,
        0x8c,0xa1,0x89,0x0d,0xbf,0xe6,0x42,0x68,0x41,0x99,0x2d,0x0f,0xb0,0x54,0xbb,0x16
    };

    public static byte[] RandomBytes(int count)
    {
        var output = new byte[count];
        var offset = 0;
        while (offset < count)
        {
            var block = Guid.NewGuid().ToByteArray();
            var take = Math.Min(block.Length, count - offset);
            Array.Copy(block, 0, output, offset, take);
            offset += take;
        }
        return output;
    }

    public static void Clear(byte[]? value)
    {
        if (value is not null) Array.Clear(value, 0, value.Length);
    }

    public static bool FixedEquals(byte[] a, byte[] b)
    {
        var mismatch = a.Length ^ b.Length;
        var length = Math.Min(a.Length, b.Length);
        for (var i = 0; i < length; i++) mismatch |= a[i] ^ b[i];
        return mismatch == 0;
    }

    public static byte[] Sha256(byte[] input)
    {
        var bitLength = (ulong)input.Length * 8;
        var total = ((input.Length + 9 + 63) / 64) * 64;
        var data = new byte[total];
        Array.Copy(input, data, input.Length);
        data[input.Length] = 0x80;
        WriteU64(data, total - 8, bitLength);
        var h = new uint[] {0x6a09e667,0xbb67ae85,0x3c6ef372,0xa54ff53a,
            0x510e527f,0x9b05688c,0x1f83d9ab,0x5be0cd19};
        var w = new uint[64];
        for (var offset = 0; offset < total; offset += 64)
        {
            for (var i = 0; i < 16; i++) w[i] = ReadU32(data, offset + i * 4);
            for (var i = 16; i < 64; i++)
            {
                var s0 = Ror(w[i-15],7) ^ Ror(w[i-15],18) ^ (w[i-15] >> 3);
                var s1 = Ror(w[i-2],17) ^ Ror(w[i-2],19) ^ (w[i-2] >> 10);
                w[i] = unchecked(w[i-16] + s0 + w[i-7] + s1);
            }
            var a=h[0];var b=h[1];var c=h[2];var d=h[3];
            var e=h[4];var f=h[5];var g=h[6];var hh=h[7];
            for (var i = 0; i < 64; i++)
            {
                var s1=Ror(e,6)^Ror(e,11)^Ror(e,25);
                var ch=(e&f)^(~e&g);
                var t1=unchecked(hh+s1+ch+ShaK[i]+w[i]);
                var s0=Ror(a,2)^Ror(a,13)^Ror(a,22);
                var maj=(a&b)^(a&c)^(b&c);
                var t2=unchecked(s0+maj);
                hh=g;g=f;f=e;e=unchecked(d+t1);d=c;c=b;b=a;a=unchecked(t1+t2);
            }
            h[0]=unchecked(h[0]+a);h[1]=unchecked(h[1]+b);
            h[2]=unchecked(h[2]+c);h[3]=unchecked(h[3]+d);
            h[4]=unchecked(h[4]+e);h[5]=unchecked(h[5]+f);
            h[6]=unchecked(h[6]+g);h[7]=unchecked(h[7]+hh);
        }
        var result = new byte[32];
        for (var i = 0; i < 8; i++) WriteU32(result, i * 4, h[i]);
        Clear(data);
        return result;
    }

    public static byte[] Pbkdf2(string password, byte[] salt, int iterations)
    {
        var key = Encoding.UTF8.GetBytes(password);
        var input = new byte[salt.Length + 4];
        Array.Copy(salt, input, salt.Length);
        input[^1] = 1;
        var u = Hmac(key, input);
        var output = Copy(u);
        for (var iteration = 1; iteration < iterations; iteration++)
        {
            var next = Hmac(key, u);
            Clear(u);
            u = next;
            for (var i = 0; i < output.Length; i++) output[i] ^= u[i];
        }
        Clear(key); Clear(input); Clear(u);
        return output;
    }

    private static byte[] Hmac(byte[] key, byte[] data)
    {
        var normalized = key.Length > 64 ? Sha256(key) : Copy(key);
        var block = new byte[64];
        Array.Copy(normalized, block, normalized.Length);
        var inner = new byte[64 + data.Length];
        var outer = new byte[96];
        for (var i = 0; i < 64; i++)
        {
            inner[i] = (byte)(block[i] ^ 0x36);
            outer[i] = (byte)(block[i] ^ 0x5c);
        }
        Array.Copy(data, 0, inner, 64, data.Length);
        var innerHash = Sha256(inner);
        Array.Copy(innerHash, 0, outer, 64, 32);
        var result = Sha256(outer);
        Clear(normalized);Clear(block);Clear(inner);Clear(outer);Clear(innerHash);
        return result;
    }

    public static void GcmEncrypt(byte[] key, byte[] nonce, byte[] plaintext,
        byte[] aad, out byte[] ciphertext, out byte[] tag)
    {
        ciphertext = Ctr(key, nonce, plaintext);
        tag = AuthenticationTag(key, nonce, aad, ciphertext);
    }

    public static bool GcmDecrypt(byte[] key, byte[] nonce, byte[] ciphertext,
        byte[] aad, byte[] tag, out byte[] plaintext)
    {
        var expected = AuthenticationTag(key, nonce, aad, ciphertext);
        var valid = FixedEquals(expected, tag);
        Clear(expected);
        plaintext = valid ? Ctr(key, nonce, ciphertext) : Array.Empty<byte>();
        return valid;
    }

    private static byte[] Ctr(byte[] key, byte[] nonce, byte[] input)
    {
        var roundKeys = ExpandKey(key);
        var counter = new byte[16];
        Array.Copy(nonce, counter, 12);
        counter[15] = 1;
        var output = new byte[input.Length];
        for (var offset = 0; offset < input.Length; offset += 16)
        {
            Increment(counter);
            var stream = EncryptBlock(counter, roundKeys);
            var count = Math.Min(16, input.Length - offset);
            for (var i = 0; i < count; i++) output[offset+i]=(byte)(input[offset+i]^stream[i]);
            Clear(stream);
        }
        Clear(roundKeys);Clear(counter);
        return output;
    }

    private static byte[] AuthenticationTag(byte[] key, byte[] nonce,
        byte[] aad, byte[] ciphertext)
    {
        var roundKeys = ExpandKey(key);
        var h = EncryptBlock(new byte[16], roundKeys);
        var y = new byte[16];
        GhashBlocks(y, h, aad);
        GhashBlocks(y, h, ciphertext);
        var lengths = new byte[16];
        WriteU64(lengths, 0, (ulong)aad.Length * 8);
        WriteU64(lengths, 8, (ulong)ciphertext.Length * 8);
        Xor(y, lengths); y = Multiply(y, h);
        var j0 = new byte[16]; Array.Copy(nonce, j0, 12); j0[15] = 1;
        var encryptedJ0 = EncryptBlock(j0, roundKeys);
        Xor(encryptedJ0, y);
        Clear(roundKeys);Clear(h);Clear(y);Clear(lengths);Clear(j0);
        return encryptedJ0;
    }

    private static void GhashBlocks(byte[] y, byte[] h, byte[] data)
    {
        for (var offset = 0; offset < data.Length; offset += 16)
        {
            var block = new byte[16];
            Array.Copy(data, offset, block, 0, Math.Min(16, data.Length-offset));
            Xor(y, block);
            var product = Multiply(y, h);
            Array.Copy(product, y, 16);
            Clear(block);Clear(product);
        }
    }

    private static byte[] Multiply(byte[] x, byte[] y)
    {
        var z = new byte[16]; var v = Copy(y);
        for (var bit = 0; bit < 128; bit++)
        {
            if ((x[bit/8] & (1 << (7-bit%8))) != 0) Xor(z, v);
            var lsb = (v[15] & 1) != 0;
            for (var i = 15; i > 0; i--)
                v[i] = (byte)((v[i] >> 1) | ((v[i-1] & 1) << 7));
            v[0] >>= 1;
            if (lsb) v[0] ^= 0xe1;
        }
        Clear(v); return z;
    }

    private static byte[] ExpandKey(byte[] key)
    {
        var expanded = new byte[240]; Array.Copy(key, expanded, 32);
        var generated = 32; byte rcon = 1; var temp = new byte[4];
        while (generated < 240)
        {
            Array.Copy(expanded, generated-4, temp, 0, 4);
            if (generated % 32 == 0)
            {
                var first=temp[0];temp[0]=SBox[temp[1]];temp[1]=SBox[temp[2]];
                temp[2]=SBox[temp[3]];temp[3]=SBox[first];temp[0]^=rcon;rcon=Xtime(rcon);
            }
            else if (generated % 32 == 16)
                for (var i=0;i<4;i++) temp[i]=SBox[temp[i]];
            for (var i=0;i<4;i++) { expanded[generated]=(byte)(expanded[generated-32]^temp[i]); generated++; }
        }
        Clear(temp); return expanded;
    }

    private static byte[] EncryptBlock(byte[] input, byte[] keys)
    {
        var state=Copy(input); AddRoundKey(state,keys,0);
        for(var round=1;round<14;round++) { SubBytes(state);ShiftRows(state);MixColumns(state);AddRoundKey(state,keys,round); }
        SubBytes(state);ShiftRows(state);AddRoundKey(state,keys,14);return state;
    }

    private static void SubBytes(byte[] s) { for(var i=0;i<16;i++) s[i]=SBox[s[i]]; }
    private static void ShiftRows(byte[] s)
    {
        var t=Copy(s);
        for(var row=0;row<4;row++) for(var col=0;col<4;col++)
            s[row+4*col]=t[row+4*((col+row)%4)];
        Clear(t);
    }
    private static void MixColumns(byte[] s)
    {
        for(var c=0;c<4;c++) { var i=c*4;var a=s[i];var b=s[i+1];var d=s[i+2];var e=s[i+3];
            s[i]=(byte)(Xtime(a)^(Xtime(b)^b)^d^e);
            s[i+1]=(byte)(a^Xtime(b)^(Xtime(d)^d)^e);
            s[i+2]=(byte)(a^b^Xtime(d)^(Xtime(e)^e));
            s[i+3]=(byte)((Xtime(a)^a)^b^d^Xtime(e)); }
    }
    private static void AddRoundKey(byte[] s,byte[] keys,int round)
    { for(var i=0;i<16;i++) s[i]^=keys[round*16+i]; }
    private static byte Xtime(byte x)=>(byte)((x<<1)^((x&0x80)!=0?0x1b:0));
    private static void Increment(byte[] c) { for(var i=15;i>=12;i--) if(++c[i]!=0) break; }
    private static void Xor(byte[] a,byte[] b) { for(var i=0;i<16;i++) a[i]^=b[i]; }
    private static uint Ror(uint x,int n)=>(x>>n)|(x<<(32-n));
    private static uint ReadU32(byte[] b,int o)=>(uint)(b[o]<<24|b[o+1]<<16|b[o+2]<<8|b[o+3]);
    private static void WriteU32(byte[] b,int o,uint v){b[o]=(byte)(v>>24);b[o+1]=(byte)(v>>16);b[o+2]=(byte)(v>>8);b[o+3]=(byte)v;}
    private static void WriteU64(byte[] b,int o,ulong v){for(var i=7;i>=0;i--){b[o+i]=(byte)v;v>>=8;}}
    private static byte[] Copy(byte[] source){var copy=new byte[source.Length];Array.Copy(source,copy,source.Length);return copy;}
}
