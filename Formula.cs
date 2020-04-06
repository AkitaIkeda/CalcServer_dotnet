using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace server_cs{
    class Formula{
        public class _FormulaPOCO{
            public double? Val { get; set; }
            public _FormulaPOCO N1 { get; set; }
            public _FormulaPOCO N2 { get; set; }
            public Operators? Op { get; set; }

            public _FormulaPOCO(double n) => Val = n;
            public _FormulaPOCO(_FormulaPOCO N1,  Operators Op, _FormulaPOCO N2){
                this.N1 = N1;
                this.Op = Op;
                this.N2 = N2;
            }
        }

        _FormulaPOCO formulaPOCO;

        private static Dictionary<Operators, Func<double, double, double>> operators = new Dictionary<Operators, Func<double, double, double>>(){
            {Operators.add, (double a, double b)=>a+b},
            {Operators.sub, (double a, double b)=>a-b},
            {Operators.mul, (double a, double b)=>a*b},
            {Operators.div, (double a, double b)=>a/b},
            {Operators.pow, (double a, double b)=>Math.Pow(a, b)},
            {Operators.mod, (double a, double b)=>a%b},
        };

        private double _calc(_FormulaPOCO f) => f.Val ??= operators[f.Op ?? throw new ArgumentNullException()](_calc(f.N1), _calc(f.N2));
        public double Calc() => _calc(this.formulaPOCO);

        public enum Operators
        {
            add = 0,
            sub = 1,
            mul = 2,
            div = 3,
            pow = 4,
            mod = 5,
        }
        public Formula(_FormulaPOCO N) => formulaPOCO = N;

        private static _FormulaPOCO _list_to_formula(List<_FormulaPOCO> nums, List<(Operators, int)> ops){
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < ops.Count;)
                {
                    if(ops[j].Item2 == i){
                        nums[j] = new _FormulaPOCO(nums[j], ops[j].Item1, nums[j+1]);
                        nums.RemoveAt(j+1);
                        ops.RemoveAt(j);
                        continue;
                    }
                    j++;
                }
            }
            return nums[0];
        }

        public static Formula strToFormula(string str) => new Formula(_str_to_list(str, 0).Item1);

        private static (_FormulaPOCO, int) _str_to_list(string str, int index){
            var opdict = new Dictionary<char, (Operators, int)>(){
                {'^', (Operators.pow, 0)},
                {'*', (Operators.mul, 1)},
                {'/', (Operators.div, 1)},
                {'%', (Operators.mod, 1)},
                {'+', (Operators.add, 2)},
                {'-', (Operators.sub, 2)},
            };
            var nums = new List<_FormulaPOCO>();
            var ops = new List<(Operators, int)>();

            str = str.Replace(" ", "");
            string tmp = "";
            for (int i = index; i < str.Length; i++)
            {
                char c = str[i];
                if(c == ')'){
                    nums.Add(new _FormulaPOCO(double.Parse(tmp)));
                    return (_list_to_formula(nums, ops), i);
                }
                if(c == '('){
                    var (fo, ind) = _str_to_list(str, i+1);
                    nums.Add(fo);
                    i = ind;
                    continue;
                }
                if(opdict.ContainsKey(c)){
                    if(i == 0){
                        tmp += c;
                        continue;
                    }
                    if(tmp == "" && str[i-1] == ')'){
                        ops.Add(opdict[c]);
                        continue;
                    }
                    nums.Add(new _FormulaPOCO(double.Parse(tmp)));
                    tmp = "";
                    ops.Add(opdict[c]);
                    continue;
                }
                tmp += c;
            }
            if(tmp != "")
                nums.Add(new _FormulaPOCO(double.Parse(tmp)));
            return (_list_to_formula(nums, ops), -1);
        }
    }
}