﻿namespace Lucene.Net.Tartarus.Snowball.Ext
{
    /// <summary>
    /// This class was automatically generated by a Snowball to Java compiler
    /// It implements the stemming algorithm defined by a snowball script.
    /// </summary>
    public class German2Stemmer : SnowballProgram
    {
        private readonly static German2Stemmer methodObject = new German2Stemmer();

        private readonly static Among[] a_0 = {
                    new Among ( "", -1, 6, "", methodObject ),
                    new Among ( "ae", 0, 2, "", methodObject ),
                    new Among ( "oe", 0, 3, "", methodObject ),
                    new Among ( "qu", 0, 5, "", methodObject ),
                    new Among ( "ue", 0, 4, "", methodObject ),
                    new Among ( "\u00DF", 0, 1, "", methodObject )
                };

        private readonly static Among[] a_1 = {
                    new Among ( "", -1, 6, "", methodObject ),
                    new Among ( "U", 0, 2, "", methodObject ),
                    new Among ( "Y", 0, 1, "", methodObject ),
                    new Among ( "\u00E4", 0, 3, "", methodObject ),
                    new Among ( "\u00F6", 0, 4, "", methodObject ),
                    new Among ( "\u00FC", 0, 5, "", methodObject )
                };

        private readonly static Among[] a_2 = {
                    new Among ( "e", -1, 1, "", methodObject ),
                    new Among ( "em", -1, 1, "", methodObject ),
                    new Among ( "en", -1, 1, "", methodObject ),
                    new Among ( "ern", -1, 1, "", methodObject ),
                    new Among ( "er", -1, 1, "", methodObject ),
                    new Among ( "s", -1, 2, "", methodObject ),
                    new Among ( "es", 5, 1, "", methodObject )
                };

        private readonly static Among[] a_3 = {
                    new Among ( "en", -1, 1, "", methodObject ),
                    new Among ( "er", -1, 1, "", methodObject ),
                    new Among ( "st", -1, 2, "", methodObject ),
                    new Among ( "est", 2, 1, "", methodObject )
                };

        private readonly static Among[] a_4 = {
                    new Among ( "ig", -1, 1, "", methodObject ),
                    new Among ( "lich", -1, 1, "", methodObject )
                };

        private readonly static Among[] a_5 = {
                    new Among ( "end", -1, 1, "", methodObject ),
                    new Among ( "ig", -1, 2, "", methodObject ),
                    new Among ( "ung", -1, 1, "", methodObject ),
                    new Among ( "lich", -1, 3, "", methodObject ),
                    new Among ( "isch", -1, 2, "", methodObject ),
                    new Among ( "ik", -1, 2, "", methodObject ),
                    new Among ( "heit", -1, 3, "", methodObject ),
                    new Among ( "keit", -1, 4, "", methodObject )
                };

        private static readonly char[] g_v = { (char)17, (char)65, (char)16, (char)1, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)8, (char)0, (char)32, (char)8 };

        private static readonly char[] g_s_ending = { (char)117, (char)30, (char)5 };

        private static readonly char[] g_st_ending = { (char)117, (char)30, (char)4 };

        private int I_x;
        private int I_p2;
        private int I_p1;

        private void copy_from(German2Stemmer other)
        {
            I_x = other.I_x;
            I_p2 = other.I_p2;
            I_p1 = other.I_p1;
            base.copy_from(other);
        }

        private bool r_prelude()
        {
            int among_var;
            int v_1;
            int v_2;
            int v_3;
            int v_4;
            int v_5;
            // (, line 28
            // test, line 30
            v_1 = cursor;
            // repeat, line 30

            while (true)
            {
                v_2 = cursor;

                do
                {
                    // goto, line 30

                    while (true)
                    {
                        v_3 = cursor;

                        do
                        {
                            // (, line 30
                            if (!(in_grouping(g_v, 97, 252)))
                            {
                                goto lab3;
                            }
                            // [, line 31
                            bra = cursor;
                            // or, line 31

                            do
                            {
                                v_4 = cursor;

                                do
                                {
                                    // (, line 31
                                    // literal, line 31
                                    if (!(eq_s(1, "u")))
                                    {
                                        goto lab5;
                                    }
                                    // ], line 31
                                    ket = cursor;
                                    if (!(in_grouping(g_v, 97, 252)))
                                    {
                                        goto lab5;
                                    }
                                    // <-, line 31
                                    slice_from("U");
                                    goto lab4;
                                } while (false);
                                lab5:
                                cursor = v_4;
                                // (, line 32
                                // literal, line 32
                                if (!(eq_s(1, "y")))
                                {
                                    goto lab3;
                                }
                                // ], line 32
                                ket = cursor;
                                if (!(in_grouping(g_v, 97, 252)))
                                {
                                    goto lab3;
                                }
                                // <-, line 32
                                slice_from("Y");
                            } while (false);
                            lab4:
                            cursor = v_3;
                            goto golab2;
                        } while (false);
                        lab3:
                        cursor = v_3;
                        if (cursor >= limit)
                        {
                            goto lab1;
                        }
                        cursor++;
                    }
                    golab2:
                    // LUCENENET NOTE: continue label is not supported directly in .NET,
                    // so we just need to add another goto to get to the end of the outer loop.
                    // See: http://stackoverflow.com/a/359449/181087

                    // Original code:
                    //continue replab0;

                    goto end_of_outer_loop;

                } while (false);
                lab1:
                cursor = v_2;
                goto replab0;
                end_of_outer_loop: { }
            }
            replab0:
            cursor = v_1;
            // repeat, line 35

            while (true)
            {
                v_5 = cursor;

                do
                {
                    // (, line 35
                    // [, line 36
                    bra = cursor;
                    // substring, line 36
                    among_var = find_among(a_0, 6);
                    if (among_var == 0)
                    {
                        goto lab7;
                    }
                    // ], line 36
                    ket = cursor;
                    switch (among_var)
                    {
                        case 0:
                            goto lab7;
                        case 1:
                            // (, line 37
                            // <-, line 37
                            slice_from("ss");
                            break;
                        case 2:
                            // (, line 38
                            // <-, line 38
                            slice_from("\u00E4");
                            break;
                        case 3:
                            // (, line 39
                            // <-, line 39
                            slice_from("\u00F6");
                            break;
                        case 4:
                            // (, line 40
                            // <-, line 40
                            slice_from("\u00FC");
                            break;
                        case 5:
                            // (, line 41
                            // hop, line 41
                            {
                                int c = cursor + 2;
                                if (0 > c || c > limit)
                                {
                                    goto lab7;
                                }
                                cursor = c;
                            }
                            break;
                        case 6:
                            // (, line 42
                            // next, line 42
                            if (cursor >= limit)
                            {
                                goto lab7;
                            }
                            cursor++;
                            break;
                    }
                    // LUCENENET NOTE: continue label is not supported directly in .NET,
                    // so we just need to add another goto to get to the end of the outer loop.
                    // See: http://stackoverflow.com/a/359449/181087

                    // Original code:
                    //continue replab6;

                    goto end_of_outer_loop_2;

                } while (false);
                lab7:
                cursor = v_5;
                goto replab6;
                end_of_outer_loop_2: { }
            }
            replab6:
            return true;
        }

        private bool r_mark_regions()
        {
            int v_1;
            // (, line 48
            I_p1 = limit;
            I_p2 = limit;
            // test, line 53
            v_1 = cursor;
            // (, line 53
            // hop, line 53
            {
                int c = cursor + 3;
                if (0 > c || c > limit)
                {
                    return false;
                }
                cursor = c;
            }
            // setmark x, line 53
            I_x = cursor;
            cursor = v_1;
            // gopast, line 55

            while (true)
            {

                do
                {
                    if (!(in_grouping(g_v, 97, 252)))
                    {
                        goto lab1;
                    }
                    goto golab0;
                } while (false);
                lab1:
                if (cursor >= limit)
                {
                    return false;
                }
                cursor++;
            }
            golab0:
            // gopast, line 55

            while (true)
            {

                do
                {
                    if (!(out_grouping(g_v, 97, 252)))
                    {
                        goto lab3;
                    }
                    goto golab2;
                } while (false);
                lab3:
                if (cursor >= limit)
                {
                    return false;
                }
                cursor++;
            }
            golab2:
            // setmark p1, line 55
            I_p1 = cursor;
            // try, line 56

            do
            {
                // (, line 56
                if (!(I_p1 < I_x))
                {
                    goto lab4;
                }
                I_p1 = I_x;
            } while (false);
            lab4:
            // gopast, line 57

            while (true)
            {

                do
                {
                    if (!(in_grouping(g_v, 97, 252)))
                    {
                        goto lab6;
                    }
                    goto golab5;
                } while (false);
                lab6:
                if (cursor >= limit)
                {
                    return false;
                }
                cursor++;
            }
            golab5:
            // gopast, line 57

            while (true)
            {

                do
                {
                    if (!(out_grouping(g_v, 97, 252)))
                    {
                        goto lab8;
                    }
                    goto golab7;
                } while (false);
                lab8:
                if (cursor >= limit)
                {
                    return false;
                }
                cursor++;
            }
            golab7:
            // setmark p2, line 57
            I_p2 = cursor;
            return true;
        }

        private bool r_postlude()
        {
            int among_var;
            int v_1;
            // repeat, line 61

            while (true)
            {
                v_1 = cursor;

                do
                {
                    // (, line 61
                    // [, line 63
                    bra = cursor;
                    // substring, line 63
                    among_var = find_among(a_1, 6);
                    if (among_var == 0)
                    {
                        goto lab1;
                    }
                    // ], line 63
                    ket = cursor;
                    switch (among_var)
                    {
                        case 0:
                            goto lab1;
                        case 1:
                            // (, line 64
                            // <-, line 64
                            slice_from("y");
                            break;
                        case 2:
                            // (, line 65
                            // <-, line 65
                            slice_from("u");
                            break;
                        case 3:
                            // (, line 66
                            // <-, line 66
                            slice_from("a");
                            break;
                        case 4:
                            // (, line 67
                            // <-, line 67
                            slice_from("o");
                            break;
                        case 5:
                            // (, line 68
                            // <-, line 68
                            slice_from("u");
                            break;
                        case 6:
                            // (, line 69
                            // next, line 69
                            if (cursor >= limit)
                            {
                                goto lab1;
                            }
                            cursor++;
                            break;
                    }
                    // LUCENENET NOTE: continue label is not supported directly in .NET,
                    // so we just need to add another goto to get to the end of the outer loop.
                    // See: http://stackoverflow.com/a/359449/181087

                    // Original code:
                    //continue replab0;

                    goto end_of_outer_loop;

                } while (false);
                lab1:
                cursor = v_1;
                goto replab0;
                end_of_outer_loop: { }
            }
            replab0:
            return true;
        }

        private bool r_R1()
        {
            if (!(I_p1 <= cursor))
            {
                return false;
            }
            return true;
        }

        private bool r_R2()
        {
            if (!(I_p2 <= cursor))
            {
                return false;
            }
            return true;
        }

        private bool r_standard_suffix()
        {
            int among_var;
            int v_1;
            int v_2;
            int v_3;
            int v_4;
            int v_5;
            int v_6;
            int v_7;
            int v_8;
            int v_9;
            // (, line 79
            // do, line 80
            v_1 = limit - cursor;

            do
            {
                // (, line 80
                // [, line 81
                ket = cursor;
                // substring, line 81
                among_var = find_among_b(a_2, 7);
                if (among_var == 0)
                {
                    goto lab0;
                }
                // ], line 81
                bra = cursor;
                // call R1, line 81
                if (!r_R1())
                {
                    goto lab0;
                }
                switch (among_var)
                {
                    case 0:
                        goto lab0;
                    case 1:
                        // (, line 83
                        // delete, line 83
                        slice_del();
                        break;
                    case 2:
                        // (, line 86
                        if (!(in_grouping_b(g_s_ending, 98, 116)))
                        {
                            goto lab0;
                        }
                        // delete, line 86
                        slice_del();
                        break;
                }
            } while (false);
            lab0:
            cursor = limit - v_1;
            // do, line 90
            v_2 = limit - cursor;

            do
            {
                // (, line 90
                // [, line 91
                ket = cursor;
                // substring, line 91
                among_var = find_among_b(a_3, 4);
                if (among_var == 0)
                {
                    goto lab1;
                }
                // ], line 91
                bra = cursor;
                // call R1, line 91
                if (!r_R1())
                {
                    goto lab1;
                }
                switch (among_var)
                {
                    case 0:
                        goto lab1;
                    case 1:
                        // (, line 93
                        // delete, line 93
                        slice_del();
                        break;
                    case 2:
                        // (, line 96
                        if (!(in_grouping_b(g_st_ending, 98, 116)))
                        {
                            goto lab1;
                        }
                        // hop, line 96
                        {
                            int c = cursor - 3;
                            if (limit_backward > c || c > limit)
                            {
                                goto lab1;
                            }
                            cursor = c;
                        }
                        // delete, line 96
                        slice_del();
                        break;
                }
            } while (false);
            lab1:
            cursor = limit - v_2;
            // do, line 100
            v_3 = limit - cursor;

            do
            {
                // (, line 100
                // [, line 101
                ket = cursor;
                // substring, line 101
                among_var = find_among_b(a_5, 8);
                if (among_var == 0)
                {
                    goto lab2;
                }
                // ], line 101
                bra = cursor;
                // call R2, line 101
                if (!r_R2())
                {
                    goto lab2;
                }
                switch (among_var)
                {
                    case 0:
                        goto lab2;
                    case 1:
                        // (, line 103
                        // delete, line 103
                        slice_del();
                        // try, line 104
                        v_4 = limit - cursor;

                        do
                        {
                            // (, line 104
                            // [, line 104
                            ket = cursor;
                            // literal, line 104
                            if (!(eq_s_b(2, "ig")))
                            {
                                cursor = limit - v_4;
                                goto lab3;
                            }
                            // ], line 104
                            bra = cursor;
                            // not, line 104
                            {
                                v_5 = limit - cursor;

                                do
                                {
                                    // literal, line 104
                                    if (!(eq_s_b(1, "e")))
                                    {
                                        goto lab4;
                                    }
                                    cursor = limit - v_4;
                                    goto lab3;
                                } while (false);
                                lab4:
                                cursor = limit - v_5;
                            }
                            // call R2, line 104
                            if (!r_R2())
                            {
                                cursor = limit - v_4;
                                goto lab3;
                            }
                            // delete, line 104
                            slice_del();
                        } while (false);
                        lab3:
                        break;
                    case 2:
                        // (, line 107
                        // not, line 107
                        {
                            v_6 = limit - cursor;

                            do
                            {
                                // literal, line 107
                                if (!(eq_s_b(1, "e")))
                                {
                                    goto lab5;
                                }
                                goto lab2;
                            } while (false);
                            lab5:
                            cursor = limit - v_6;
                        }
                        // delete, line 107
                        slice_del();
                        break;
                    case 3:
                        // (, line 110
                        // delete, line 110
                        slice_del();
                        // try, line 111
                        v_7 = limit - cursor;

                        do
                        {
                            // (, line 111
                            // [, line 112
                            ket = cursor;
                            // or, line 112

                            do
                            {
                                v_8 = limit - cursor;

                                do
                                {
                                    // literal, line 112
                                    if (!(eq_s_b(2, "er")))
                                    {
                                        goto lab8;
                                    }
                                    goto lab7;
                                } while (false);
                                lab8:
                                cursor = limit - v_8;
                                // literal, line 112
                                if (!(eq_s_b(2, "en")))
                                {
                                    cursor = limit - v_7;
                                    goto lab6;
                                }
                            } while (false);
                            lab7:
                            // ], line 112
                            bra = cursor;
                            // call R1, line 112
                            if (!r_R1())
                            {
                                cursor = limit - v_7;
                                goto lab6;
                            }
                            // delete, line 112
                            slice_del();
                        } while (false);
                        lab6:
                        break;
                    case 4:
                        // (, line 116
                        // delete, line 116
                        slice_del();
                        // try, line 117
                        v_9 = limit - cursor;

                        do
                        {
                            // (, line 117
                            // [, line 118
                            ket = cursor;
                            // substring, line 118
                            among_var = find_among_b(a_4, 2);
                            if (among_var == 0)
                            {
                                cursor = limit - v_9;
                                goto lab9;
                            }
                            // ], line 118
                            bra = cursor;
                            // call R2, line 118
                            if (!r_R2())
                            {
                                cursor = limit - v_9;
                                goto lab9;
                            }
                            switch (among_var)
                            {
                                case 0:
                                    cursor = limit - v_9;
                                    goto lab9;
                                case 1:
                                    // (, line 120
                                    // delete, line 120
                                    slice_del();
                                    break;
                            }
                        } while (false);
                        lab9:
                        break;
                }
            } while (false);
            lab2:
            cursor = limit - v_3;
            return true;
        }


        public override bool Stem()
        {
            int v_1;
            int v_2;
            int v_3;
            int v_4;
            // (, line 130
            // do, line 131
            v_1 = cursor;

            do
            {
                // call prelude, line 131
                if (!r_prelude())
                {
                    goto lab0;
                }
            } while (false);
            lab0:
            cursor = v_1;
            // do, line 132
            v_2 = cursor;
            do
            {
                // call mark_regions, line 132
                if (!r_mark_regions())
                {
                    goto lab1;
                }
            } while (false);
            lab1:
            cursor = v_2;
            // backwards, line 133
            limit_backward = cursor; cursor = limit;
            // do, line 134
            v_3 = limit - cursor;
            do
            {
                // call standard_suffix, line 134
                if (!r_standard_suffix())
                {
                    goto lab2;
                }
            } while (false);
            lab2:
            cursor = limit - v_3;
            cursor = limit_backward;                    // do, line 135
            v_4 = cursor;
            do
            {
                // call postlude, line 135
                if (!r_postlude())
                {
                    goto lab3;
                }
            } while (false);
            lab3:
            cursor = v_4;
            return true;
        }

        public override bool Equals(object o)
        {
            return o is German2Stemmer;
        }

        public override int GetHashCode()
        {
            return this.GetType().FullName.GetHashCode();
        }
    }
}
