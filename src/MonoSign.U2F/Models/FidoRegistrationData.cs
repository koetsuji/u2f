﻿using System;
using System.IO;
using Newtonsoft.Json;

namespace MonoSign.U2F
{
    [JsonConverter(typeof(FidoRegistrationDataConverter))]
	public class FidoRegistrationData
	{
		private const byte RegistrationReservedByte = 0x05;

		/// <summary>
		/// The (uncompressed) x,y-representation of a curve point on the P-256 NIST elliptic curve.
		/// </summary>
		public FidoPublicKey UserPublicKey { get; set; }

		/// <summary>
		/// A handle that allows the U2F token to identify the generated key pair.
		/// </summary>
		public FidoKeyHandle KeyHandle { get; set; }

		public FidoAttestationCertificate AttestationCertificate { get; set; }

		/// <summary>
		/// A ECDSA signature (on P-256)
		/// </summary>
		public FidoSignature Signature { get; set; }

	    public FidoRegistrationData()
	    {
	    }

		private FidoRegistrationData(FidoPublicKey userPublicKey, FidoKeyHandle keyHandle,
						   FidoAttestationCertificate attestationCertificate,
						   FidoSignature signature)
		{
			UserPublicKey = userPublicKey;
			KeyHandle = keyHandle;
			AttestationCertificate = attestationCertificate;
			Signature = signature;
		}

		public static FidoRegistrationData FromWebSafeBase64(string webSafeBase64)
		{
			return FromBytes(WebSafeBase64Converter.FromBase64String(webSafeBase64));
		}

		public static FidoRegistrationData FromBytes(byte[] rawRegistrationData)
		{
		    if (rawRegistrationData == null) throw new ArgumentNullException("rawRegistrationData");

		    using (var mem = new MemoryStream(rawRegistrationData))
			{
				return FromStream(mem);
			}
		}

        private static FidoRegistrationData FromStream(Stream stream)
		{
		    if (stream == null) throw new ArgumentNullException("stream");

		    using (var binaryReader = new BinaryReader(stream))
			{
				var reservedByte = binaryReader.ReadByte();

				if (reservedByte != RegistrationReservedByte)
				{
					throw new InvalidOperationException(String.Format(
						"Incorrect value of reserved byte (expected: 0x{0:X2} but was: 0x{1:X1})",
						RegistrationReservedByte, reservedByte));
				}

				try
				{
					var publicKeyBytes = binaryReader.ReadBytes(65);
				    var keyHandleLength = binaryReader.ReadByte();
				    var keyHandleBytes = binaryReader.ReadBytes(keyHandleLength);

					var nextChunkSize = (int)(binaryReader.BaseStream.Length - binaryReader.BaseStream.Position);
					var certificatePosition = binaryReader.BaseStream.Position;
					var certBytes = binaryReader.ReadBytes(nextChunkSize);
					var certificate = new FidoAttestationCertificate(certBytes);
					var certSize = certificate.Certificate.GetEncoded().Length;

					binaryReader.BaseStream.Position = certificatePosition + certSize;
					nextChunkSize = (int)(binaryReader.BaseStream.Length - binaryReader.BaseStream.Position);

					var signatureBytes = binaryReader.ReadBytes(nextChunkSize);

					var registerResponse = new FidoRegistrationData(
						new FidoPublicKey(publicKeyBytes),
						new FidoKeyHandle(keyHandleBytes),
						certificate,
						new FidoSignature(signatureBytes));

					return registerResponse;
				}
				catch (Exception ex)
				{
					var message = String.Format("Error parsing registration data ({0})", ex.Message);
					throw new InvalidOperationException(message, ex);
				}
			}
		}

        public string ToWebSafeBase64()
        {
            return WebSafeBase64Converter.ToBase64String(ToBytes());
        }

        public byte[] ToBytes()
        {
            using (var mem = new MemoryStream())
            {
                ToStream(mem);
                return mem.ToArray();
            }
        }

        public void ToStream(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            using (var binaryWriter = new BinaryWriter(stream))
            {
                binaryWriter.Write(RegistrationReservedByte);

                var publicKey = UserPublicKey.ToByteArray();
                binaryWriter.Write(publicKey);
                var keyHandle = KeyHandle.ToByteArray();
                binaryWriter.Write((byte)keyHandle.Length);
                binaryWriter.Write(keyHandle);

                var certBytes = AttestationCertificate.Certificate.GetEncoded();
                binaryWriter.Write(certBytes);

                binaryWriter.Write(Signature.ToByteArray());
            }
        }

        public void Validate()
	    {
            if (UserPublicKey == null)
                throw new InvalidOperationException("UserPublicKey is missing in " + typeof(FidoRegistrationData).Name);

            if (KeyHandle == null)
                throw new InvalidOperationException("KeyHandle is missing in " + typeof(FidoRegistrationData).Name);

            if (Signature == null)
                throw new InvalidOperationException("Signature is missing in " + typeof(FidoRegistrationData).Name);

            if (AttestationCertificate == null)
                throw new InvalidOperationException("AttestationCertificate is missing in " + typeof(FidoRegistrationData).Name);

            UserPublicKey.Validate();
            KeyHandle.Validate();
            Signature.Validate();
            AttestationCertificate.Validate();
	    }
    }
}
