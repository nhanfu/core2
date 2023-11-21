namespace Core.Extensions
{
    public static class NumberExtensions
    {
        public static decimal RoundUp(this decimal value, int decimals)
        {
            return decimal.Round(value, decimals, MidpointRounding.AwayFromZero);
        }

        public static decimal ToRealDecimal(this decimal value)
        {
            return (decimal)Convert.ChangeType(value, typeof(decimal));
        }

        public static string NumberToText(this decimal inputNumber)
        {
            bool suffix = true;
            string[] unitNumbers = new string[] { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };
            string[] placeValues = new string[] { "", "nghìn", "triệu", "tỷ" };
            bool isNegative = false;
            string sNumber = inputNumber.ToString("#");
            double number = Convert.ToDouble(sNumber);

            if (number < 0)
            {
                number = -number;
                sNumber = number.ToString();
                isNegative = true;
            }

            int ones, tens, hundreds;
            int positionDigit = sNumber.Length;
            string result = " ";

            if (positionDigit == 0)
            {
                result = unitNumbers[0] + result;
            }
            else
            {
                int placeValue = 0;

                while (positionDigit > 0)
                {
                    tens = hundreds = -1;
                    ones = Convert.ToInt32(sNumber.Substring(positionDigit - 1, 1));
                    positionDigit--;
                    if (positionDigit > 0)
                    {
                        tens = Convert.ToInt32(sNumber.Substring(positionDigit - 1, 1));
                        positionDigit--;
                        if (positionDigit > 0)
                        {
                            hundreds = Convert.ToInt32(sNumber.Substring(positionDigit - 1, 1));
                            positionDigit--;
                        }
                    }

                    if ((ones > 0) || (tens > 0) || (hundreds > 0) || (placeValue == 3))
                    {
                        result = placeValues[placeValue] + result;
                    }

                    placeValue++;
                    if (placeValue > 3)
                    {
                        placeValue = 1;
                    }

                    if ((ones == 1) && (tens > 1))
                    {
                        result = "một " + result;
                    }
                    else
                    {
                        if ((ones == 5) && (tens > 0))
                        {
                            result = "lăm " + result;
                        }
                        else if (ones > 0)
                        {
                            result = unitNumbers[ones] + " " + result;
                        }
                    }
                    if (tens < 0)
                    {
                        break;
                    }
                    else
                    {
                        if ((tens == 0) && (ones > 0))
                        {
                            result = "lẻ " + result;
                        }

                        if (tens == 1)
                        {
                            result = "mười " + result;
                        }

                        if (tens > 1)
                        {
                            result = unitNumbers[tens] + " mươi " + result;
                        }
                    }
                    if (hundreds < 0)
                    {
                        break;
                    }
                    else
                    {
                        if ((hundreds > 0) || (tens > 0) || (ones > 0))
                        {
                            result = unitNumbers[hundreds] + " trăm " + result;
                        }
                    }
                    result = " " + result;
                }
            }
            result = result.Trim();
            if (isNegative)
            {
                result = "Âm " + result;
            }

            return result + (suffix ? " đồng chẵn" : "");
        }
    }
}
