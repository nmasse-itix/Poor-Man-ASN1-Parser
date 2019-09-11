using System;
using System.Collections;
using System.IO;
using System.Text;

public class PoorManAsn1ParserTest
{
	static public void Main(String[] args) 
	{
		if (args.Length != 1) {
			Console.WriteLine("Usage: PoorManAsn1ParserTest.exe <file>");
			return;
		}

        try
        {
            FileStream f = File.Open(args[0], FileMode.Open);
            int length = Convert.ToInt32(f.Length);
            byte[] rawasn1 = new byte[length];
            f.Read(rawasn1, 0, length);

            PoorManAsn1Parser parser = new PoorManAsn1Parser();
            Object obj = parser.Parse(rawasn1, 0);
            Dump(obj, "");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
        }
	}

	static private void Dump(Object o, String indent) {
        if (o == null)
        {
            Console.WriteLine("{0}null", indent);
        } 
        else if (o is String)
        {
			Console.WriteLine("{0}{1}", indent, o as String);
		}
        else if (o is byte[])
        {
            StringBuilder sb = new StringBuilder(indent);
            sb.Append("[");
            foreach (byte b in (o as byte[]))
            {
                sb.AppendFormat("{0:x}", b);
            }
            sb.Append("]");
            Console.WriteLine(sb.ToString());
        }
        else if (o is IEnumerable)
        {
            Console.WriteLine(indent + "{");
            String indent2 = indent + "  ";
            foreach (Object elem in (o as IEnumerable))
            {
                Dump(elem, indent2);
            }
            Console.WriteLine(indent + "}");
        }
        else 
        {
			Console.WriteLine("{0}<unknown> {1}", indent, o.GetType().FullName);
		}
	}
}

internal class PoorManAsn1Parser
{
	public PoorManAsn1Parser() 
	{
	}

    private void Length(byte[] rawasn1, int offset, out int length, out int lengthLength)
    {
        length = 0;
        lengthLength = 0;

        byte tag = rawasn1[offset];
        if ((tag & 0x1F) == 0x1F)
        {
            throw new NotImplementedException("Tag too long");
        }
        offset++;
        
        if (rawasn1[offset] == 0x80)
        {
            throw new NotImplementedException("Length not defined");
        }

        if ((rawasn1[offset] & 0x80) == 0) // Length on one byte
        {
            length = rawasn1[offset];
            lengthLength = 1;
        }
        else
        {
            lengthLength = rawasn1[offset] & 0x7F;
            if (lengthLength > 4) // Overflow ?
            {
                throw new NotImplementedException("Length too long");
            }

            for (int i = 1; i < lengthLength + 1; i++)
            {
                length <<= 7;
                length |= rawasn1[offset + i];
            }

            lengthLength++;
        }
    }

	public Object Parse(byte[] rawasn1, int offset) 
	{
		Object value = null;
		
		byte tag = rawasn1[offset];
		bool isPrimitive = ((tag & 0x20) == 0);
        byte label = (byte) (tag & 0x17);

        int dataLength;
        int lengthLength;
        Length(rawasn1, offset, out dataLength, out lengthLength);

        Console.WriteLine("offset = {0}, tag = {1:x}, length = {2}, lengthLength = {3}", offset, tag, dataLength, lengthLength);

        int dataOffset = offset + lengthLength + 1;

        if (label == 0x1F)
        {
            throw new NotImplementedException("Tag too long");
        }

		if (isPrimitive)
		{
			switch (label) 
			{
			    case 0x01: // Bool
                    value = (rawasn1[dataOffset] != 0);
                    break;
                case 0x02: // Integer
                    value = "<Integer>"; // TODO
                    break;
                case 0x03: // Bitstring
                    byte[] bitString = new byte[dataLength - 1];
                    Array.Copy(rawasn1, dataOffset + 1, bitString, 0, dataLength - 1);
                    value = bitString;
                    break;
                case 0x04: // Octet string
                    byte[] octetString = new byte[dataLength];
                    Array.Copy(rawasn1, dataOffset, octetString, 0, dataLength);
                    value = octetString;
                    break;
                case 0x05: // Null
				    break;
                case 0x06: // OID
                    value = "<OID>"; // TODO
                    break;
                case 0x13: // PrintableString
                case 0x16: // IA5String
                    value = Encoding.ASCII.GetString(rawasn1, dataOffset, dataLength);
                    break;
                default:
                    throw new NotImplementedException(String.Format("Unknown type: {0}", label));
			}
		} 
		else 
		{
			IList list = new ArrayList();
            int thisObjectOffset = dataOffset;
            do
            {
                int thisObjectLength;
                int thisObjectLengthLength;
                Length(rawasn1, thisObjectOffset, out thisObjectLength, out thisObjectLengthLength);
                list.Add(Parse(rawasn1, thisObjectOffset));
                thisObjectOffset += thisObjectLength + thisObjectLengthLength + 1;
            } while (thisObjectOffset < dataOffset + dataLength);

            value = list;
		}

        return value;
	}
	
}
