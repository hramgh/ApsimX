﻿namespace Models.Surface
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Interfaces;
    using Models.PMF;
    using Models.Soils;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// # [Name]
    /// The surface organic matter model.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType=typeof(Zone))]
    public class SurfaceOrganicMatter : ModelCollectionFromResource, ISurfaceOrganicMatter
    {
        /// <summary>Link to the soil component</summary>
        [Link]
        private Soil soil = null;

        /// <summary>Link to the summary component</summary>
        [Link]
        private ISummary summary = null;

        /// <summary>Link to the weather component</summary>
        [Link]
        private IWeather weather = null;

        /// <summary>Link to Apsim's solute manager module.</summary>
        [Link]
        private SoluteManager solutes = null;

        /// <summary>Link to the soil N model</summary>
        [Link]
        private INutrient SoilNitrogen = null;

        /// <summary>Gets or sets the residue types.</summary>
        [ChildLink]
        private ResidueTypes ResidueTypes = null;

        /// <summary>
        /// Number of pools into which carbon is grouped.
        /// Currently there are three, indexed as follows:
        /// 0 = carbohydrate
        /// 1 = cellulose
        /// 2 = lignin.
        /// </summary>
        private static int maxFr = 3;

        /// <summary>The surf om</summary>
        private List<SurfOrganicMatterType> SurfOM;

        /// <summary>The number surfom</summary>
        private int numSurfom = 0;

        /// <summary>The irrig</summary>
        private double irrig;

        /// <summary>The cumeos</summary>
        private double cumeos;

        /// <summary>The determinant of whether a residue type contributes to the calculation of contact factor (1 or 0)</summary>
        private int[] cf_contrib = new int[0];

        /// <summary>Fraction of Carbon in plant material (0-1)</summary>
        private double[] C_fract = new double[0];

        /// <summary>The carbohydrate fraction in fom C pool (0-1)</summary>
        private double[,] frPoolC = new double[maxFr, 0];

        /// <summary>The carbohydrate fraction in fom N pool (0-1)</summary>
        private double[,] frPoolN = new double[maxFr, 0];

        /// <summary>The carbohydrate fraction in fom P pool (0-1)</summary>
        private double[,] frPoolP = new double[maxFr, 0];

        /// <summary>Ammonium component of residue (ppm)</summary>
        private double[] nh4ppm = new double[0];

        /// <summary>Nitrate component of residue (ppm)</summary>
        private double[] no3ppm = new double[0];

        /// <summary>Phosphate component of residue (ppm)</summary>
        private double[] po4ppm = new double[0];

        /// <summary>Specific area of residue (ha/kg)</summary>
        private double[] specific_area = new double[0]; 

        /// <summary>Constructor</summary>
        public SurfaceOrganicMatter()
            : base()
        {
            // Set default values for some properties
            CriticalResidueWeight = 2000;
            OptimumDecompTemp = 20;
            MaxCumulativeEOS = 20;
            CNRatioDecompCoeff = 0.277;
            CNRatioDecompThreshold = 25;
            TotalLeachRain = 25;
            MinRainToLeach = 10;
            CriticalMinimumOrganicC = 0.004;
            DefaultCPRatio = 0.0;
            DefaultStandingFraction = 0.0;
            StandingExtinctCoeff = 0.5;
            FractionFaecesAdded = 0.5;
            SurfOM = new List<SurfOrganicMatterType>();
        }

        /// <summary>Gets or sets the name of initial residue pool</summary>
        [Summary]
        [Description("Name of initial residue pool")]
        [Units("")]
        public string InitialResidueName { get; set; }

        /// <summary>Gets or sets the type of initial residue pool</summary>
        [Summary]
        [Description("Type of initial residue pool")]
        [Display(Type = DisplayType.ResidueName)]
        [Units("")]
        public string InitialResidueType { get; set; }

        /// <summary>Gets or sets the mass of initial residue pool</summary>
        [Summary]
        [Description("Mass of initial surface residue (kg/ha)")]
        [Units("kg/ha")]
        public double InitialResidueMass { get; set; }

        /// <summary>Gets or sets the standing fraction of initial residue pool</summary>
        [Summary]
        [Description("Standing fraction (0-1)")]
        [Units("0-1")]
        public double InitialStandingFraction { get; set; }

        /// <summary>Gets or sets the Carbon:Phosphorus ratio.</summary>
        [Summary]
        [Description("Carbon:Phosphorus ratio")]
        [Units("g/g")]
        public double InitialCPR { get; set; }

        /// <summary>Gets or sets the initial Carbon:Nitrogen ratio.</summary>
        [Summary]
        [Description("Carbon:Nitrogen ratio (g/g)")]
        [Units("g/g")]
        public double InitialCNR { get; set; }

        /// <summary>critical residue weight below which Thorburn"s cover factor equals one</summary>
        /// <value>The critical residue weight.</value>
        [Units("")]
        public double CriticalResidueWeight { get; set; }

        /// <summary>temperature at which decomp reaches optimum (oC)</summary>
        /// <value>The optimum decomp temporary.</value>
        [Units("oC")]
        public double OptimumDecompTemp { get; set; }

        /// <summary>cumeos at which decomp rate becomes zero. (mm H2O)</summary>
        /// <value>The maximum cumulative eos.</value>
        [Units("")]
        public double MaxCumulativeEOS { get; set; }

        /// <summary>Coefficient to determine the magnitude of C:N effects on decomposition of residue</summary>
        /// <value>The cn ratio decomp coeff.</value>
        [Units("")]
        public double CNRatioDecompCoeff { get; set; }

        /// <summary>C:N above which decomposition rate of residue declines</summary>
        /// <value>The cn ratio decomp threshold.</value>
        [Units("")]
        public double CNRatioDecompThreshold { get; set; }

        /// <summary>total amount of "leaching" rain to remove all soluble N from surfom</summary>
        /// <value>The total leach rain.</value>
        [Units("")]
        public double TotalLeachRain { get; set; }

        /// <summary>threshold rainfall amount for leaching to occur</summary>
        /// <value>The minimum rain to leach.</value>
        [Units("")]
        public double MinRainToLeach { get; set; }

        /// <summary>
        /// critical minimum org C below which potential decomposition rate is 100% (to avoid numerical imprecision)
        /// </summary>
        /// <value>The critical minimum organic c.</value>
        [Units("")]
        public double CriticalMinimumOrganicC { get; set; }

        /// <summary>Default C:P ratio</summary>
        /// <value>The default cp ratio.</value>
        [Units("")]
        public double DefaultCPRatio { get; set; }

        /// <summary>Default standing fraction for initial residue pools</summary>
        /// <value>The default standing fraction.</value>
        [Units("")]
        public double DefaultStandingFraction { get; set; }

        /// <summary>extinction coefficient for standing residues</summary>
        /// <value>The standing extinct coeff.</value>
        [Units("")]
        public double StandingExtinctCoeff { get; set; }

        /// <summary>fraction of incoming faeces to add</summary>
        /// <value>The fraction faeces added.</value>
        [Bounds(Lower = 0.0, Upper = 0.0)]
        [Units("0-1")]
        public double FractionFaecesAdded { get; set; }

        /// <summary>The labile_p</summary>
        [Units("")]
        public double[] labile_p = null;

        /// <summary>Total mass of all surface organic materials</summary>
        /// <value>The Surface OM Weight.</value>
        [Units("kg/ha")]
        public double Wt { get { return SumSurfOMStandingLying(SurfOM, x => x.amount); } }

        /// <summary>Total mass of all surface organic carbon</summary>
        /// <value>The surfaceom_c.</value>
        [Summary]
        [Description("Carbon content")]
        [Units("kg/ha")]
        public double C { get { return SumSurfOMStandingLying(SurfOM, x => x.C); } }

        /// <summary>Total mass of all surface organic nitrogen</summary>
        /// <value>The surfaceom_n.</value>
        [Summary]
        [Description("Nitrogen content")]
        [Units("kg/ha")]
        public double N { get { return SumSurfOMStandingLying(SurfOM, x => x.N); } }

        /// <summary>Total mass of all surface organic phosphor</summary>
        /// <value>The surfaceom_p.</value>
        [Summary]
        [Description("Phosphorus content")]
        [Units("kg/ha")]
        public double P { get { return SumSurfOMStandingLying(SurfOM, x => x.P); } }

        /// <summary>Total mass of nitrate</summary>
        /// <value>The surfaceom_no3.</value>
        [Units("kg/ha")]
        public double NO3 { get { return SumSurfOM(SurfOM, x => x.no3); } }

        /// <summary>Total mass of ammonium</summary>
        /// <value>The surfaceom_nh4.</value>
        [Units("kg/ha")]
        public double NH4 { get { return SumSurfOM(SurfOM, x => x.nh4); } }

        /// <summary>Total mass of labile phosphorus</summary>
        /// <value>The surfaceom_labile_p.</value>
        [Units("kg/ha")]
        public double LabileP { get { return SumSurfOM(SurfOM, x => x.po4); } }

        /// <summary>Fraction of ground covered by all surface OMs</summary>
        /// <value>The surfaceom_cover.</value>
        [Description("Fraction of ground covered by all surface OMs")]
        [Units("m^2/m^2")]
        public double Cover { get { return CoverTotal(); } }

        /// <summary>Temperature factor for decomposition</summary>
        /// <value>The tf.</value>
        [Units("0-1")]
        public double tf { get { return TemperatureFactor(); } }

        /// <summary>Contact factor for decomposition</summary>
        /// <value>The cf.</value>
        [Units("0-1")]
        public double cf { get { return ContactFactor(); } }

        /// <summary>Gets the wf.</summary>
        /// <value>The wf.</value>
        [Units("0-1")]
        public double wf { get { return MoistureFactor(); } }

        /// <summary>Get the weight of the given SOM pool</summary>
        /// <param name="pool">Name of the pool to get the weight from.</param>
        /// <returns>The weight of the given pool</returns>
        public double GetWeightFromPool(string pool)
        {
            var SomType = SurfOM.Find(x => x.name.Equals(pool, StringComparison.CurrentCultureIgnoreCase));
            return SumOMFractionType(SomType.Standing, y => y.amount) +
                SumOMFractionType(SomType.Lying, y => y.amount);
        }

        /// <summary>Return a list of known residue types names</summary>
        public List<string> ResidueTypeNames()
        {
            if (ResidueTypes == null && Children.Count > 0)
            {
                // This happens when user clicks on SurfaceOrganicMatter in tree.
                // Links haven't been resolved yet
                ResidueTypes = Children[0] as ResidueTypes;
            }
            return ResidueTypes.Names;
        }

        /// <summary>
        /// "cover1" and "cover2" are numbers between 0 and 1 which
        /// indicate what fraction of sunlight is intercepted by the
        /// foliage of plants.  This function returns a number between
        /// 0 and 1 indicating the fraction of sunlight intercepted
        /// when "cover1" is combined with "cover2", i.e. both sets of
        /// plants are present.
        /// </summary>
        /// <param name="cover1">The cover1.</param>
        /// <param name="cover2">The cover2.</param>
        /// <returns></returns>
        private double AddCover(double cover1, double cover2)
        {
            double bare = (1.0 - cover1) * (1.0 - cover2);
            return 1.0 - bare;
        }

        /// <summary>The apsim bound warning error</summary>
        private const string ApsimBoundWarningError =
    @"'{0}' out of bounds!
     {1} < {2} < {3} evaluates 'FALSE'";

        /// <summary>Bound_check_real_vars the specified value.</summary>
        /// <param name="value">The value.</param>
        /// <param name="lower">The lower.</param>
        /// <param name="upper">The upper.</param>
        /// <param name="vname">The vname.</param>
        private void Bound_check_real_var(double value, double lower, double upper, string vname)
        {
            if (MathUtilities.IsLessThan(value, lower) || MathUtilities.IsGreaterThan(value, upper))
                summary.WriteWarning(this, string.Format(ApsimBoundWarningError, vname, lower, value, upper));
        }

        /// <summary>Reals_are_equals the specified first.</summary>
        /// <param name="first">The first.</param>
        /// <param name="second">The second.</param>
        /// <returns></returns>
        private bool reals_are_equal(double first, double second)
        {
            return Math.Abs(first - second) < 2 * double.Epsilon;
        }

        /// <summary>Gets the cumulative index real.</summary>
        /// <param name="cum_sum">The cum_sum.</param>
        /// <param name="array">The array.</param>
        /// <returns></returns>
        private int GetCumulativeIndexReal(double cum_sum, double[] array)
        {
            int sizeOf = array.Length - 1;
            double cum = 0;
            for (int i = 0; i < sizeOf; i++)
                if ((cum += array[i]) >= cum_sum)
                    return i;
            return sizeOf;
        }
        
        /// <summary>To the array.</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="str">The string.</param>
        /// <returns></returns>
        protected T[] ToArray<T>(string str)
        {
            string[] temp;

            if (str == null || str == string.Empty)
                temp = new string[0];
            else
                temp = str.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);

            MethodInfo parser = null;
            if (typeof(T) != typeof(string))
                parser = typeof(T).GetMethod("Parse", new Type[] { typeof(string) });

            T[] result = new T[temp.Length];

            for (int i = 0; i < result.Length; i++)
                result[i] = parser == null ? (T)(object)temp[i] : (T)parser.Invoke(null, new[] { temp[i] });

            return result;
        }

        /// <summary>Sum2s the d array.</summary>
        /// <param name="_2Darray">The _2 darray.</param>
        /// <returns></returns>
        double Sum2DArray(double[,] _2Darray)
        {
            double result = 0;
            foreach (double f in _2Darray)
                result += f;
            return result;
        }

        /// <summary>Sums the surf om standing lying.</summary>
        /// <param name="var">The variable.</param>
        /// <param name="func">The function.</param>
        /// <returns></returns>
        double SumSurfOMStandingLying(List<SurfOrganicMatterType> var, Func<OMFractionType, double> func)
        {
            return var.Sum<SurfOrganicMatterType>(x => SumSurfOMStandingLying(x, func));
        }

        /// <summary>Sums the surf om standing lying.</summary>
        /// <param name="var">The variable.</param>
        /// <param name="func">The function.</param>
        /// <returns></returns>
        double SumSurfOMStandingLying(SurfOrganicMatterType var, Func<OMFractionType, double> func)
        {
            return var.Lying.Sum<OMFractionType>(func) + var.Standing.Sum<OMFractionType>(func);
        }

        /// <summary>Sums the surf om.</summary>
        /// <param name="var">The variable.</param>
        /// <param name="func">The function.</param>
        /// <returns></returns>
        double SumSurfOM(List<SurfOrganicMatterType> var, Func<SurfOrganicMatterType, double> func)
        {
            return var.Sum<SurfOrganicMatterType>(func);
        }

        /// <summary>Sums the type of the om fraction.</summary>
        /// <param name="var">The variable.</param>
        /// <param name="func">The function.</param>
        /// <returns></returns>
        double SumOMFractionType(OMFractionType[] var, Func<OMFractionType, double> func)
        {
            return var.Sum<OMFractionType>(func);
        }

        /// <summary>Residues the mass.</summary>
        /// <param name="type">The type.</param>
        /// <param name="func">The function.</param>
        /// <returns></returns>
        double ResidueMass(string type, Func<OMFractionType, double> func)
        {
            int SOMNo = GetResidueNumber(type);
            if (SOMNo > 0)
                return SurfOM.Sum<SurfOrganicMatterType>(x => x.Lying.Sum<OMFractionType>(func) + x.Standing.Sum<OMFractionType>(func));
            else
                throw new ApsimXException(this, "No organic matter called " + type + " present");
        }

        /// <summary>Initialise residue module</summary>
        private void SurfomReset()
        {
            ZeroVariables();
            ReadParam();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Data">The data.</param>
        public delegate void FOMPoolDelegate(FOMPoolType Data);

        /// <summary>Occurs when [incorp fom pool].</summary>
        public event FOMPoolDelegate IncorpFOMPool;

        /// <summary>
        /// 
        /// </summary>
        public class SurfaceOrganicMatterPoolType
        {
            /// <summary>The name</summary>
            public string Name = string.Empty;
            /// <summary>The organic matter type</summary>
            public string OrganicMatterType = string.Empty;
            /// <summary>The pot decomp rate</summary>
            public double PotDecompRate;
            /// <summary>The no3</summary>
            public double no3;
            /// <summary>The NH4</summary>
            public double nh4;
            /// <summary>The po4</summary>
            public double po4;
            /// <summary>The standing fraction</summary>
            public FOMType[] StandingFraction;
            /// <summary>The lying fraction</summary>
            public FOMType[] LyingFraction;
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Reset();
        }

        /// <summary>Called when [reset].</summary>
        public void Reset()
        {
            SurfOM = new List<SurfOrganicMatterType>();
            irrig = 0;
            cumeos = 0;
            ZeroVariables();
            SurfomReset();
        }

        /// <summary>Get irrigation information from an Irrigated event.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The data.</param>
        [EventSubscribe("Irrigated")]
        private void OnIrrigated(object sender, IrrigationApplicationType data)
        {
            irrig += data.Amount;
        }

        /// <summary>Called when [biomass removed].</summary>
        /// <param name="BiomassRemoved">The biomass removed.</param>
        [EventSubscribe("BiomassRemoved")]
        private void OnBiomassRemoved(BiomassRemovedType BiomassRemoved)
        {
            SurfOMOnBiomassRemoved(BiomassRemoved);
        }

        /// <summary>Return the potential residue decomposition for today.</summary>
        /// <returns></returns>
        public SurfaceOrganicMatterDecompType PotentialDecomposition()
        {
            double precip = weather.Rain + irrig;
            if (precip > 4.0)
                cumeos = soil.SoilWater.Eos - precip;
            else
                cumeos = this.cumeos + soil.SoilWater.Eos - precip;
            cumeos = Math.Max(cumeos, 0.0);

            if (precip >= MinRainToLeach)
                Leach(precip);

            irrig = 0.0; // reset irrigation log now that we have used that information;

            return SendPotDecompEvent();
        }

        /// <summary>Actual surface organic matter decomposition. Calculated by SoilNitrogen.</summary>
        /// <value>The actual som decomp.</value>
        private SurfaceOrganicMatterDecompType ActualSOMDecomp { get; set; }

        /// <summary>Do the daily residue decomposition for today.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoSurfaceOrganicMatterDecomposition")]
        private void OnDoSurfaceOrganicMatterDecomposition(object sender, EventArgs args)
        {
            ActualSOMDecomp = SoilNitrogen.CalculateActualSOMDecomp();
            if (ActualSOMDecomp != null)
                DecomposeSurfom(ActualSOMDecomp);
        }

        /// <summary>Model has been loaded.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The event data</param>
        [EventSubscribe("Loaded")]
        private void OnLoaded(object sender, EventArgs args)
        {
            Children.ForEach(child => child.IsHidden = true);
        }

        /// <summary>
        /// 
        /// </summary>
        public class AddFaecesType
        {
            /// <summary>The defaecations</summary>
            public double Defaecations;
            /// <summary>The volume per defaecation</summary>
            public double VolumePerDefaecation;
            /// <summary>The area per defaecation</summary>
            public double AreaPerDefaecation;
            /// <summary>The eccentricity</summary>
            public double Eccentricity;
            /// <summary>The om weight</summary>
            public double OMWeight;
            /// <summary>The omn</summary>
            public double OMN;
            /// <summary>The omp</summary>
            public double OMP;
            /// <summary>The oms</summary>
            public double OMS;
            /// <summary>The om ash alk</summary>
            public double OMAshAlk;
            /// <summary>The n o3 n</summary>
            public double NO3N;
            /// <summary>The n h4 n</summary>
            public double NH4N;
            /// <summary>The poxp</summary>
            public double POXP;
            /// <summary>The s o4 s</summary>
            public double SO4S;
        }

        /// <summary>Called when [add faeces].</summary>
        /// <param name="data">The data.</param>
        public void OnAddFaeces(AddFaecesType data) { AddFaeces(data); }

        /// <summary>Surfoms the total state.</summary>
        /// <returns></returns>
        private OMFractionType SurfomTotalState()
        {
            OMFractionType SOMstate = new OMFractionType();

            if (SurfOM == null || SurfOM.Count == 0)
                return SOMstate;

            SOMstate.N = SurfOM.Sum<SurfOrganicMatterType>(x => x.no3 + x.nh4);
            SOMstate.P = SurfOM.Sum<SurfOrganicMatterType>(x => x.po4);

            for (int pool = 0; pool < maxFr; pool++)
            {
                SOMstate.amount += SurfOM.Sum<SurfOrganicMatterType>(x => x.Lying[pool].amount + x.Standing[pool].amount);
                SOMstate.C += SurfOM.Sum<SurfOrganicMatterType>(x => x.Lying[pool].C + x.Standing[pool].C);
                SOMstate.N += SurfOM.Sum<SurfOrganicMatterType>(x => x.Lying[pool].N + x.Standing[pool].N);
                SOMstate.P += SurfOM.Sum<SurfOrganicMatterType>(x => x.Lying[pool].P + x.Standing[pool].P);
            }

            return SOMstate;
        }

        /// <summary>Set all variables in this module to zero.</summary>
        private void ZeroVariables()
        {
            cumeos = 0;
            irrig = 0;
        }

        /// <summary>
        /// Read in all parameters from parameter file
        /// <para>
        /// This now just modifies the inputs and puts them into global structs, reading in is handled by .NET
        /// </para>
        /// </summary>
        private void ReadParam()
        {
            double totC = 0;  // total C in residue;
            double totN = 0;  // total N in residue;
            double totP = 0;  // total P in residue;

            if (InitialResidueMass > 0.0)
            {
                // Normally the residue shouldn't already exist, and we
                // will need to add it, and normally this should result in SOMNo == i
                // but it's safest not to assume this
                int SOMNo = GetResidueNumber(InitialResidueName);
                if (SOMNo < 0)
                {
                    SOMNo = AddNewSurfOM(InitialResidueName, InitialResidueType);
                }

                // convert the ppm figures into kg/ha;
                SurfOM[SOMNo].no3 += MathUtilities.Divide(no3ppm[SOMNo], 1000000.0, 0.0) * InitialResidueMass;
                SurfOM[SOMNo].nh4 += MathUtilities.Divide(nh4ppm[SOMNo], 1000000.0, 0.0) * InitialResidueMass;
                SurfOM[SOMNo].po4 += MathUtilities.Divide(po4ppm[SOMNo], 1000000.0, 0.0) * InitialResidueMass;

                totC = InitialResidueMass * C_fract[SOMNo];
                totN = MathUtilities.Divide(totC, InitialCNR, 0.0);

                // If a C:P ratio is not provided, use the default
                double cpr = double.IsNaN(InitialCPR) ? DefaultCPRatio : InitialCPR;
                totP = MathUtilities.Divide(totC, cpr, 0.0);

                double standFract = double.IsNaN(InitialStandingFraction) ? DefaultCPRatio : InitialStandingFraction;

                for (int j = 0; j < maxFr; j++)
                {
                    SurfOM[SOMNo].Standing[j].amount += InitialResidueMass * frPoolC[j, SOMNo] * standFract;
                    SurfOM[SOMNo].Standing[j].C += totC * frPoolC[j, SOMNo] * standFract;
                    SurfOM[SOMNo].Standing[j].N += totN * frPoolN[j, SOMNo] * standFract;
                    SurfOM[SOMNo].Standing[j].P += totP * frPoolP[j, SOMNo] * standFract;

                    SurfOM[SOMNo].Lying[j].amount += InitialResidueMass * frPoolC[j, SOMNo] * (1.0 - standFract);
                    SurfOM[SOMNo].Lying[j].C += totC * frPoolC[j, SOMNo] * (1.0 - standFract);
                    SurfOM[SOMNo].Lying[j].N += totN * frPoolN[j, SOMNo] * (1.0 - standFract);
                    SurfOM[SOMNo].Lying[j].P += totP * frPoolP[j, SOMNo] * (1.0 - standFract);
                }
            }
        }


        /// <summary>Get the solutes number</summary>
        /// <param name="surfomname">The surfomname.</param>
        /// <returns></returns>
        private int GetResidueNumber(string surfomname)
        {
            if (SurfOM == null)
                return -1;

            for (int i = 0; i < SurfOM.Count; i++)
                if (SurfOM[i].name.Equals(surfomname, StringComparison.CurrentCultureIgnoreCase))
                    return i;

            return -1;
        }

        /// <summary>
        /// Performs manure decomposition taking into account environmental;
        /// and manure factors (independant to soil N but N balance can modify;
        /// actual decomposition rates if desired by N model - this is possible;
        /// because pools are not updated until end of time step - see post routine)
        /// </summary>
        /// <param name="c_pot_decomp">The c_pot_decomp.</param>
        /// <param name="n_pot_decomp">The n_pot_decomp.</param>
        /// <param name="p_pot_decomp">The p_pot_decomp.</param>
        private void PotDecomp(out double[] c_pot_decomp, out double[] n_pot_decomp, out double[] p_pot_decomp)
        {
            // (these pools are not updated until end of time step - see post routine)
            c_pot_decomp = new double[numSurfom];
            n_pot_decomp = new double[numSurfom];
            p_pot_decomp = new double[numSurfom];

            double
                mf = MoistureFactor(),     // moisture factor for decomp (0-1)
                tf = TemperatureFactor(),  // temperature factor for decomp (0-1)
                cf = ContactFactor();      // manure/soil contact factor for decomp (0-1)

            for (int residue = 0; residue < numSurfom; residue++)
            {
                double cnrf = CNRatioFactor(residue);    // C:N factor for decomp (0-1) for surfom under consideration

                double
                    Fdecomp = -1,       // decomposition fraction for the given surfom
                    sumC = SurfOM[residue].Lying.Sum<OMFractionType>(x => x.C);

                if (sumC < CriticalMinimumOrganicC)
                {
                    // Residue wt is sufficiently low to suggest decomposing all;
                    // material to avoid low numbers which can cause problems;
                    // with numerical precision;
                    Fdecomp = 1;
                }
                else
                {
                    // Calculate today"s decomposition  as a fraction of potential rate;
                    Fdecomp = SurfOM[residue].PotDecompRate * mf * tf * cnrf * cf;
                }

                // Now calculate pool decompositions for this residue;
                c_pot_decomp[residue] = Fdecomp * sumC;
                n_pot_decomp[residue] = Fdecomp * SurfOM[residue].Lying.Sum<OMFractionType>(x => x.N);
                p_pot_decomp[residue] = Fdecomp * SurfOM[residue].Lying.Sum<OMFractionType>(x => x.P);
            }
        }

        /// <summary>
        /// Calculate temperature factor for manure decomposition (0-1).
        /// <para>
        /// Notes;
        /// The temperature factor is a simple function of the square of daily
        /// average temperature.  The user only needs to give an optimum temperature
        /// and the code will back calculate the necessary coefficient at compile time.
        /// </para>
        /// </summary>
        /// <returns>temperature factor</returns>
        private double TemperatureFactor()
        {
            double
                ave_temp = MathUtilities.Divide((weather.MaxT + weather.MinT), 2.0, 0.0);  // today"s average air temp (oC)

            if (ave_temp > 0.0)
                return MathUtilities.Bound(
                    (double)Math.Pow(MathUtilities.Divide(ave_temp, OptimumDecompTemp, 0.0), 2.0),
                    0.0,
                    1.0);
            else
                return 0;
        }

        /// <summary>Calculate manure/soil contact factor for manure decomposition (0-1).</summary>
        /// <returns></returns>
        private double ContactFactor()
        {
            double effSurfomMass = 0;  // Total residue wt across all instances;

            // Sum the effective mass of surface residues considering lying fraction only.
            // The "effective" weight takes into account the haystack effect and is governed by the;
            // cf_contrib factor (ini file).  ie some residue types do not contribute to the haystack effect.

            for (int residue = 0; residue < numSurfom; residue++)
                effSurfomMass += SurfOM[residue].Lying.Sum<OMFractionType>(x => x.amount) * cf_contrib[residue];

            if (effSurfomMass <= CriticalResidueWeight)
                return 1.0;
            else
                return MathUtilities.Bound(MathUtilities.Divide(CriticalResidueWeight, effSurfomMass, 0.0), 0, 1);
        }

        /// <summary>Calculate C:N factor for decomposition (0-1).</summary>
        /// <param name="residue">residue number</param>
        /// <returns>C:N factor for decomposition(0-1)</returns>
        private double CNRatioFactor(int residue)
        {
            if (residue < 0)
                return 1;
            else
            {
                // Note: C:N ratio factor only based on lying fraction
                double
                    total_C = SurfOM[residue].Lying.Sum<OMFractionType>(x => x.C),        // organic C component of this residue (kg/ha)
                    total_N = SurfOM[residue].Lying.Sum<OMFractionType>(x => x.N),        // organic N component of this residue (kg/ha)
                    total_mineral_n = SurfOM[residue].no3 + SurfOM[residue].nh4,          // mineral N component of this surfom (no3 + nh4)(kg/ha)
                    cnr = MathUtilities.Divide(total_C, (total_N + total_mineral_n), 0.0); // C:N for this residue  (unitless)

                // As C:N increases above optcn cnrf decreases exponentially toward zero;
                // As C:N decreases below optcn cnrf is constrained to one;

                if (CNRatioDecompThreshold == 0)
                    return 1;
                else
                    return MathUtilities.Bound(
                        (double)Math.Exp(-CNRatioDecompCoeff * ((cnr - CNRatioDecompThreshold) / CNRatioDecompThreshold)),
                        0.0,
                        1.0);

            }
        }

        /// <summary>Calculate moisture factor for manure decomposition (0-1).</summary>
        /// <returns></returns>
        private double MoistureFactor()
        {
            return MathUtilities.Bound(1.0 - MathUtilities.Divide(cumeos, MaxCumulativeEOS, 0.0), 0.0, 1.0);
        }

        /// <summary>Calculate total cover</summary>
        /// <returns></returns>
        private double CoverTotal()
        {
            double combinedCover = 0;  // effective combined cover(0-1)

            for (int i = 0; i < numSurfom; i++)
                combinedCover = AddCover(combinedCover, CoverOfSOM(i));

            return combinedCover;
        }

        /// <summary>
        /// Remove mineral N and P from surfom with leaching rainfall and;
        /// pass to Soil N and Soil P modules.
        /// </summary>
        /// <param name="leachRain">The leach rain.</param>
        private void Leach(double leachRain)
        {

            double nh4Incorp;
            double no3Incorp;
            double po4Incorp;

            // Apply leaching fraction to all mineral pools and put all mineral NO3,NH4 and PO4 into top layer;
            double leaching_fr = MathUtilities.Bound(MathUtilities.Divide(leachRain, TotalLeachRain, 0.0), 0.0, 1.0);
            no3Incorp = SurfOM.Sum<SurfOrganicMatterType>(x => x.no3) * leaching_fr;
            nh4Incorp = SurfOM.Sum<SurfOrganicMatterType>(x => x.nh4) * leaching_fr;
            po4Incorp = SurfOM.Sum<SurfOrganicMatterType>(x => x.po4) * leaching_fr;


            // If neccessary, Send the mineral N & P leached to the Soil N&P modules;
            if (no3Incorp > 0.0 || nh4Incorp > 0.0 || po4Incorp > 0.0)
            {
                solutes.AddToLayer(0, "NH4", SoluteManager.SoluteSetterType.Soil, nh4Incorp);
                solutes.AddToLayer(0, "NO3", SoluteManager.SoluteSetterType.Soil, no3Incorp);
            }

            for (int i = 0; i < numSurfom; i++)
            {
                SurfOM[i].no3 = SurfOM[i].no3 * (1.0 - leaching_fr);
                SurfOM[i].nh4 = SurfOM[i].nh4 * (1.0 - leaching_fr);
                SurfOM[i].po4 = SurfOM[i].po4 * (1.0 - leaching_fr);
            }
        }

        /// <summary>Notify other modules of the potential to decompose.</summary>
        /// <returns></returns>
        private SurfaceOrganicMatterDecompType SendPotDecompEvent()
        {

            SurfaceOrganicMatterDecompType SOMDecomp = new SurfaceOrganicMatterDecompType()
            {
                Pool = new SurfaceOrganicMatterDecompPoolType[numSurfom]
            };

            if (numSurfom <= 0)
                return SOMDecomp;

            double[] c_pot_decomp, n_pot_decomp, p_pot_decomp;
            PotDecomp(out c_pot_decomp, out n_pot_decomp, out p_pot_decomp);

            for (int residue = 0; residue < numSurfom; residue++)
                SOMDecomp.Pool[residue] = new SurfaceOrganicMatterDecompPoolType()
                {
                    Name = SurfOM[residue].name,
                    OrganicMatterType = SurfOM[residue].OrganicMatterType,

                    FOM = new FOMType()
                    {
                        amount = MathUtilities.Divide(c_pot_decomp[residue], C_fract[residue], 0.0),
                        C = c_pot_decomp[residue],
                        N = n_pot_decomp[residue],
                        P = p_pot_decomp[residue],
                        AshAlk = 0.0
                    }
                };
            return SOMDecomp;
        }

        /// <summary>Decomposes the surfom.</summary>
        /// <param name="SOMDecomp">The som decomp.</param>
        private void DecomposeSurfom(SurfaceOrganicMatterDecompType SOMDecomp)
        {
            int numSurfom = SOMDecomp.Pool.Length;          // local surfom counter from received event;
            int residue_no;                                 // Index into the global array;
            double[] cPotDecomp = new double[numSurfom];  // pot amount of C to decompose (kg/ha)
            double[] nPotDecomp = new double[numSurfom];  // pot amount of N to decompose (kg/ha)
            double[] pTotDecomp = new double[numSurfom];  // pot amount of P to decompose (kg/ha)
            double totCDecomp;    // total amount of c to decompose;
            double totNDecomp;    // total amount of c to decompose;
            double totPDecomp;    // total amount of c to decompose;

            double SOMcnr = 0;
            double SOMc = 0;
            double SOMn = 0;

            // calculate potential decompostion of C, N, and P;
            PotDecomp(out cPotDecomp, out nPotDecomp, out pTotDecomp);

            for (int counter = 0; counter < numSurfom; counter++)
            {
                totCDecomp = SOMDecomp.Pool[counter].FOM.C;
                totNDecomp = SOMDecomp.Pool[counter].FOM.N;

                residue_no = GetResidueNumber(SOMDecomp.Pool[counter].Name);
                Bound_check_real_var(totNDecomp, 0.0, nPotDecomp[residue_no], "total n decomposition");

                SOMc = SurfOM[residue_no].Standing.Sum<OMFractionType>(x => x.C) + SurfOM[residue_no].Lying.Sum<OMFractionType>(x => x.C);
                SOMn = SurfOM[residue_no].Standing.Sum<OMFractionType>(x => x.N) + SurfOM[residue_no].Lying.Sum<OMFractionType>(x => x.N);

                SOMcnr = MathUtilities.Divide(SOMc, SOMn, 0.0);

                const double acceptableErr = 1e-4;

                if (reals_are_equal(totCDecomp, 0.0) && reals_are_equal(totNDecomp, 0.0)) { }
                // all OK - nothing happening;
                else if (totCDecomp > cPotDecomp[residue_no] + acceptableErr)
                    throw new ApsimXException(this, "SurfaceOM - C decomposition exceeds potential rate");
                else if (totNDecomp > nPotDecomp[residue_no] + acceptableErr)
                    throw new ApsimXException(this, "SurfaceOM - N decomposition exceeds potential rate");

                totPDecomp = totCDecomp * MathUtilities.Divide(pTotDecomp[residue_no], cPotDecomp[residue_no], 0.0);
                Decomp(totCDecomp, totNDecomp, totPDecomp, residue_no);
            }
        }

        /// <summary>Performs updating of pools due to surfom decomposition</summary>
        /// <param name="C_decomp">C to be decomposed</param>
        /// <param name="N_decomp">N to be decomposed</param>
        /// <param name="P_decomp">P to be decomposed</param>
        /// <param name="residue">residue number being dealt with</param>
        private void Decomp(double C_decomp, double N_decomp, double P_decomp, int residue)
        {

            double Fdecomp;  // decomposing fraction;

            // do C
            Fdecomp = MathUtilities.Bound(MathUtilities.Divide(C_decomp, SurfOM[residue].Lying.Sum<OMFractionType>(x => x.C), 0.0), 0.0, 1.0);
            for (int i = 0; i < maxFr; i++)
            {
                SurfOM[residue].Lying[i].C = SurfOM[residue].Lying[i].C * (1.0 - Fdecomp);
                SurfOM[residue].Lying[i].amount = SurfOM[residue].Lying[i].amount * (1.0 - Fdecomp);
            }

            // do N
            Fdecomp = MathUtilities.Divide(N_decomp, SurfOM[residue].Lying.Sum<OMFractionType>(x => x.N), 0.0);
            for (int i = 0; i < maxFr; i++)
                SurfOM[residue].Lying[i].N = SurfOM[residue].Lying[i].N * (1.0 - Fdecomp);

            // do P
            Fdecomp = MathUtilities.Divide(P_decomp, SurfOM[residue].Lying.Sum<OMFractionType>(x => x.P), 0.0);
            for (int i = 0; i < maxFr; i++)
                SurfOM[residue].Lying[i].P = SurfOM[residue].Lying[i].P * (1.0 - Fdecomp);
        }

        /// <summary>
        /// Calculate surfom incorporation as a result of tillage and update;
        /// residue and N pools.
        /// </summary>
        /// <param name="actionType">Type of the action.</param>
        /// <param name="fIncorp">The f incorp.</param>
        /// <param name="tillageDepth">The tillage depth.</param>
        private void Incorp(string actionType, double fIncorp, double tillageDepth)
        // ================================================================
        {            
            int deepestLayer;
            int nLayers = soil.Thickness.Length;
            double F_incorp_layer = 0;
            double[] residueIncorpFraction = new double[nLayers];
            double layerIncorpDepth;
            double[,] CPool = new double[maxFr, nLayers];  // total C in each Om fraction and layer (from all surfOM"s) incorporated;
            double[,] NPool = new double[maxFr, nLayers];  // total N in each Om fraction and layer (from all surfOM"s) incorporated;
            double[,] PPool = new double[maxFr, nLayers];  // total P in each Om fraction and layer (from all surfOM"s) incorporated;
            double[,] AshAlkPool = new double[maxFr, nLayers]; // total AshAlk in each Om fraction and layer (from all surfOM"s) incorporated;
            double[] no3 = new double[nLayers]; // total no3 to go into each soil layer (from all surfOM"s)
            double[] nh4 = new double[nLayers]; // total nh4 to go into each soil layer (from all surfOM"s)
            double[] po4 = new double[nLayers]; // total po4 to go into each soil layer (from all surfOM"s)
            FOMPoolType FPoolProfile = new FOMPoolType();

            fIncorp = MathUtilities.Bound(fIncorp, 0.0, 1.0);

            deepestLayer = GetCumulativeIndexReal(tillageDepth, soil.Thickness);

            double cumDepth = 0.0;

            for (int layer = 0; layer <= deepestLayer; layer++)
            {
                for (int residue = 0; residue < numSurfom; residue++)
                {
                    double depthToGo = tillageDepth - cumDepth;
                    layerIncorpDepth = Math.Min(depthToGo, soil.Thickness[layer]);
                    F_incorp_layer = MathUtilities.Divide(layerIncorpDepth, tillageDepth, 0.0);
                    for (int i = 0; i < maxFr; i++)
                    {
                        CPool[i, layer] += (SurfOM[residue].Lying[i].C + SurfOM[residue].Standing[i].C) * fIncorp * F_incorp_layer;
                        NPool[i, layer] += (SurfOM[residue].Lying[i].N + SurfOM[residue].Standing[i].N) * fIncorp * F_incorp_layer;
                        PPool[i, layer] += (SurfOM[residue].Lying[i].P + SurfOM[residue].Standing[i].P) * fIncorp * F_incorp_layer;
                    }
                    no3[layer] += SurfOM[residue].no3 * fIncorp * F_incorp_layer;
                    nh4[layer] += SurfOM[residue].nh4 * fIncorp * F_incorp_layer;
                    po4[layer] += SurfOM[residue].po4 * fIncorp * F_incorp_layer;
                }
                cumDepth = cumDepth + soil.Thickness[layer];
                residueIncorpFraction[layer] = F_incorp_layer;
            }

            if (Sum2DArray(CPool) > 0.0)
            {
                FPoolProfile.Layer = new FOMPoolLayerType[deepestLayer + 1];

                for (int layer = 0; layer <= deepestLayer; layer++)
                {
                    FPoolProfile.Layer[layer] = new FOMPoolLayerType()
                    {
                        thickness = soil.Thickness[layer],
                        no3 = no3[layer],
                        nh4 = nh4[layer],
                        po4 = po4[layer],
                        Pool = new FOMType[maxFr]
                    };

                    for (int i = 0; i < maxFr; i++)
                        FPoolProfile.Layer[layer].Pool[i] = new FOMType()
                        {
                            C = CPool[i, layer],
                            N = NPool[i, layer],
                            P = PPool[i, layer],
                            AshAlk = AshAlkPool[i, layer]
                        };
                }
                if (IncorpFOMPool != null)
                    IncorpFOMPool.Invoke(FPoolProfile);
            }

            for (int pool = 0; pool < maxFr; pool++)
            {
                for (int i = 0; i < SurfOM.Count; i++)
                {
                    SurfOM[i].Lying[pool].amount = SurfOM[i].Lying[pool].amount * (1.0 - fIncorp);
                    SurfOM[i].Standing[pool].amount = SurfOM[i].Standing[pool].amount * (1.0 - fIncorp);

                    SurfOM[i].Lying[pool].C = SurfOM[i].Lying[pool].C * (1.0 - fIncorp);
                    SurfOM[i].Standing[pool].C = SurfOM[i].Standing[pool].C * (1.0 - fIncorp);

                    SurfOM[i].Lying[pool].N = SurfOM[i].Lying[pool].N * (1.0 - fIncorp);
                    SurfOM[i].Standing[pool].N = SurfOM[i].Standing[pool].N * (1.0 - fIncorp);

                    SurfOM[i].Lying[pool].P = SurfOM[i].Lying[pool].P * (1.0 - fIncorp);
                    SurfOM[i].Standing[pool].P = SurfOM[i].Standing[pool].P * (1.0 - fIncorp);
                }
            }

            for (int i = 0; i < SurfOM.Count; i++)
            {
                SurfOM[i].no3 *= (1.0 - fIncorp);
                SurfOM[i].nh4 *= (1.0 - fIncorp);
                SurfOM[i].po4 *= (1.0 - fIncorp);
            }
        }

        /// <summary>Adds the new surf om.</summary>
        /// <param name="newName">The new name.</param>
        /// <param name="newType">The new type.</param>
        /// <returns></returns>
        private int AddNewSurfOM(string newName, string newType)
        {
            if (SurfOM == null)
                SurfOM = new List<SurfOrganicMatterType>();

            SurfOM.Add(new SurfOrganicMatterType(newName, newType));
            numSurfom = SurfOM.Count;
            Array.Resize(ref cf_contrib, numSurfom);
            Array.Resize(ref C_fract, numSurfom);
            Array.Resize(ref nh4ppm, numSurfom);
            Array.Resize(ref no3ppm, numSurfom);
            Array.Resize(ref po4ppm, numSurfom);
            Array.Resize(ref specific_area, numSurfom);
            frPoolC = IncreasePoolArray(frPoolC);
            frPoolN = IncreasePoolArray(frPoolN);
            frPoolP = IncreasePoolArray(frPoolP);

            int SOMNo = numSurfom - 1;
            ReadTypeSpecificConstants(SurfOM[SOMNo].OrganicMatterType, SOMNo, out SurfOM[SOMNo].PotDecompRate);
            return SOMNo;
        }

        /// <summary>
        /// Adds excreta in response to an AddFaeces event
        /// This is a still the minimalist version, providing
        /// an alternative to using add_surfaceom directly
        /// </summary>
        /// <param name="data">structure holding description of the added faeces</param>
        public void AddFaeces(AddFaecesType data)
        {
            string Manure = "manure";
            Add((double)(data.OMWeight * FractionFaecesAdded),
                         (double)(data.OMN * FractionFaecesAdded),
                         (double)(data.OMP * FractionFaecesAdded),
                         Manure, "");
        }

        /// <summary>
        /// Reads type-specific residue constants from ini-file and places them in c. constants;
        /// </summary>
        /// <param name="surfom_type">The surfom_type.</param>
        /// <param name="i">The i.</param>
        /// <param name="pot_decomp_rate">The pot_decomp_rate.</param>
        private void ReadTypeSpecificConstants(string surfom_type, int i, out double pot_decomp_rate)
        {
            ResidueType thistype = ResidueTypes.GetResidueType(surfom_type);
            if (thistype == null)
                throw new ApsimXException(this, "Cannot find residue type description for '" + surfom_type + "'");

            C_fract[i] = MathUtilities.Bound(thistype.fraction_C, 0.0, 1.0);
            po4ppm[i] = MathUtilities.Bound(thistype.po4ppm, 0.0, 1000.0);
            nh4ppm[i] = MathUtilities.Bound(thistype.nh4ppm, 0.0, 2000.0);
            no3ppm[i] = MathUtilities.Bound(thistype.no3ppm, 0.0, 1000.0);
            specific_area[i] = MathUtilities.Bound(thistype.specific_area, 0.0, 0.01);
            cf_contrib[i] = Math.Max(Math.Min(thistype.cf_contrib, 1), 0);
            pot_decomp_rate = MathUtilities.Bound(thistype.pot_decomp_rate, 0.0, 1.0);

            if (thistype.fr_c.Length != thistype.fr_n.Length || thistype.fr_n.Length != thistype.fr_p.Length)
                throw new ApsimXException(this, "Error reading in fr_c/n/p values, inconsistent array lengths");

            for (int j = 0; j < thistype.fr_c.Length; j++)
            {
                frPoolC[j, i] = thistype.fr_c[j];
                frPoolN[j, i] = thistype.fr_n[j];
                frPoolP[j, i] = thistype.fr_p[j];
            }
        }

        /// <summary>
        /// This function returns the fraction of the soil surface covered by;
        /// residue according to the relationship from Gregory (1982).
        /// <para>Notes;</para>
        /// <para>Gregory"s equation is of the form;</para>
        /// <para>        Fc = 1.0 - exp (- Am * M)   where Fc = Fraction covered;</para>
        /// <para>                                          Am = Specific Area (ha/kg)</para>
        /// <para>                                           M = Mulching rate (kg/ha)</para>
        /// <para>This residue model keeps track of the total residue area and so we can
        /// substitute this value (area residue/unit area) for the product_of Am * M.</para>
        /// </summary>
        /// <param name="SOMindex">The so mindex.</param>
        /// <returns></returns>
        private double CoverOfSOM(int SOMindex)
        {
            double areaLying = 0;
            double areaStanding = 0;
            for (int i = 0; i < maxFr; i++)
            {
                areaLying += SurfOM[SOMindex].Lying[i].amount * specific_area[SOMindex];
                areaStanding += SurfOM[SOMindex].Standing[i].amount * specific_area[SOMindex];
            }
            double F_Cover = AddCover(1.0 - (double)Math.Exp(-areaLying), 1.0 - (double)Math.Exp(-(StandingExtinctCoeff) * areaStanding));
            return MathUtilities.Bound(F_Cover, 0.0, 1.0);
        }

        /// <summary>Get information on surfom added from the crops</summary>
        /// <param name="BiomassRemoved">The biomass removed.</param>
        private void SurfOMOnBiomassRemoved(BiomassRemovedType BiomassRemoved)
        {
            double
                surfomAdded = 0,   // amount of residue added (kg/ha)
                surfomNAdded = 0, // amount of residue N added (kg/ha)
                surfomPAdded = 0; // amount of residue N added (kg/ha)

            if (BiomassRemoved.fraction_to_residue.Sum() != 0)
            {
                // Find the amount of surfom to be added today;
                for (int i = 0; i < BiomassRemoved.fraction_to_residue.Length; i++)
                    surfomAdded += BiomassRemoved.dlt_crop_dm[i] * BiomassRemoved.fraction_to_residue[i];

                if (surfomAdded > 0.0)
                {
                    // Find the amount of N & added in surfom today;
                    for (int i = 0; i < BiomassRemoved.dlt_dm_p.Length; i++)
                    {
                        surfomPAdded += BiomassRemoved.dlt_dm_p[i] * BiomassRemoved.fraction_to_residue[i];
                        surfomNAdded += BiomassRemoved.dlt_dm_n[i] * BiomassRemoved.fraction_to_residue[i];
                    }

                    Add(surfomAdded, surfomNAdded, surfomPAdded, BiomassRemoved.crop_type, "");
                }
            }
        }

        /// <summary>Adds material to the surface organic matter pool.</summary>
        /// <param name="mass">The amount of biomass added (kg/ha).</param>
        /// <param name="N">The amount of N added (ppm).</param>
        /// <param name="P">The amount of P added (ppm).</param>
        /// <param name="type">Type of the biomass.</param>
        /// <param name="name">Name of the biomass written to summary file</param>
        public void Add(double mass, double N, double P, string type, string name)
        {
            // Assume the "cropType" is the unique name.  Now check whether this unique "name" already exists in the system.
            int SOMNo = GetResidueNumber(type);
            if (SOMNo < 0)
                SOMNo = AddNewSurfOM(type, type);

            // convert the ppm figures into kg/ha;
            SurfOM[SOMNo].no3 += MathUtilities.Divide(no3ppm[SOMNo], 1000000.0, 0.0) * mass;
            SurfOM[SOMNo].nh4 += MathUtilities.Divide(nh4ppm[SOMNo], 1000000.0, 0.0) * mass;
            SurfOM[SOMNo].po4 += MathUtilities.Divide(po4ppm[SOMNo], 1000000.0, 0.0) * mass;

            // Assume all surfom added is in the LYING pool, ie No STANDING component;
            for (int i = 0; i < maxFr; i++)
            {
                SurfOM[SOMNo].Lying[i].amount += mass * frPoolC[i, SOMNo];
                SurfOM[SOMNo].Lying[i].C += mass * C_fract[SOMNo] * frPoolC[i, SOMNo];
                SurfOM[SOMNo].Lying[i].N += N * frPoolN[i, SOMNo];
                SurfOM[SOMNo].Lying[i].P += P * frPoolP[i, SOMNo];
            }            
        }

        /// <summary>Resize2s the d array.</summary>
        /// <param name="original">The original.</param>
        /// <returns></returns>
        private double[,] IncreasePoolArray (double[,] original)
        {
            double[,] newArray = new double[original.GetLength(0), original.GetLength(1)+1];

            for (int x = 0; x < original.GetLength(0); x++)
                for (int y = 0; y < original.GetLength(1); y++)
                    newArray[x, y] = original[x, y];

            return newArray;
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable]
        private class SurfOrganicMatterType
        {
            /// <summary>The name</summary>
            public string name;
            /// <summary>The organic matter type</summary>
            public string OrganicMatterType;
            /// <summary>The pot decomp rate</summary>
            public double PotDecompRate;
            /// <summary>The no3</summary>
            public double no3;
            /// <summary>The NH4</summary>
            public double nh4;
            /// <summary>The po4</summary>
            public double po4;
            /// <summary>The standing</summary>
            public OMFractionType[] Standing;
            /// <summary>The lying</summary>
            public OMFractionType[] Lying;

            /// <summary>Initializes a new instance of the <see cref="SurfOrganicMatterType"/> class.</summary>
            public SurfOrganicMatterType()
            {
                name = null;
                OrganicMatterType = null;
                PotDecompRate = 0;
                no3 = 0;
                nh4 = 0;
                po4 = 0;
                Standing = new OMFractionType[maxFr];
                Lying = new OMFractionType[maxFr];

                for (int i = 0; i < maxFr; i++)
                {
                    Lying[i] = new OMFractionType();
                    Standing[i] = new OMFractionType();
                }
            }

            /// <summary>Initializes a new instance of the <see cref="SurfOrganicMatterType"/> class.</summary>
            /// <param name="name">The name.</param>
            /// <param name="type">The type.</param>
            public SurfOrganicMatterType(string name, string type)
                : this()
            {
                this.name = name;
                OrganicMatterType = type;
            }
        }

        /// <summary>Type carrying information about the CNP composition of an organic matter fraction</summary>
        [Serializable]
        private class OMFractionType
        {
            /// <summary>The amount</summary>
            public double amount;
            /// <summary>The c</summary>
            public double C;
            /// <summary>The n</summary>
            public double N;
            /// <summary>The p</summary>
            public double P;

            /// <summary>Initializes a new instance of the <see cref="OMFractionType"/> class.</summary>
            public OMFractionType()
            {
                amount = 0;
                C = 0;
                N = 0;
                P = 0;
            }
        }
    }
}