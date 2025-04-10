using Assets.Scripts;
using Assets.Scripts.Flight.Sim;
using ModApi.Craft;
using ModApi.Flight;
using ModApi.Flight.Sim;
using ModApi.Planet;
using ModApi.Ui.Inspector;
using UnityEngine;

public class CraftInfo
{
    public GroupModel GroupModel = new GroupModel("Craft Info");
    public int partCount;
    public float craftMass;
    public IPlanetNode planetNode;
    public string trajectoryType = "Unset State";
    public string biome;
    public ICraftNode PlayerCraftNode { get; private set; }
    public Vector3d velocity { get; private set; }
    internal double planetRadius => planetNode.PlanetData.Radius;
    public double craftApoapsis => PlayerCraftNode.Orbit.ApoapsisDistance - planetRadius;
    public double craftPeriapsis => PlayerCraftNode.Orbit.PeriapsisDistance - planetRadius;
    internal double atmosphereHeight => planetNode.PlanetData.AtmosphereData.Height;
    internal bool hasTakenOff;
    internal IFlightScene flightScene => Game.Instance.FlightScene;

    private double[] atmosphereicFlight = { 0.07, 0.3, 0.5 };
    private double[] vacuumFlight = { 0.01, 0.02, 0.04 };

    // Debating if I want to include the different flyings, and instead just have one
    // Leaving flying fro planes, ascending/descending for rockets
    const string _onSurface = "Surface";
    const string _grounded = "Grounded";
    const string _landed = "Landed";
    const string _onWater = "Sea";
    const string _submerged = "Submerged";
    const string _spashDown = "Splashed Down";
    const string _ascending = "Ascending";
    const string _descending = "Descending";
    const string _flying = "Flying";
    const string _flyingLow = "Flying Low";
    const string _flyingHigh = "Flying High";
    const string _subOrbital = "Suborbital";
    const string _orbitLow = "Low Orbit";
    const string _orbitMed = "Medium Orbit";
    const string _orbitHigh = "High Orbit";
    const string _escape = "On Escape";

    public void Initialize()
    {
        CreateGroupModel();
    }

    private void CreateGroupModel()
    {
        GroupModel.Add(new TextModel("Flight State", () => trajectoryType));
        GroupModel.Add(new TextModel("Periods", () => string.Format("{0} {1}", -2 * Mathf.PI / planetNode.PlanetData.AngularVelocity, PlayerCraftNode.Orbit.Period)));
        GroupModel.Add(new TextModel("Apoapsis", () => craftApoapsis.ToString()));
        GroupModel.Add(new TextModel("Periapsis", () => craftPeriapsis.ToString()));
    }

    public void UpdateValues()
    {
        if (!GroupModel.Visible && GroupModel != null)
            return;
        PlayerCraftNode = flightScene.CraftNode;
        partCount = PlayerCraftNode.CraftPartCount;
        craftMass = PlayerCraftNode.CraftMass;
        GetVesselState(PlayerCraftNode);
    }

    private void GetVesselState(ICraftNode craftNode)
    {
        planetNode = craftNode.Parent;
        biome = flightScene.CraftBiomeData.BiomeName == null ? "No Biome Data" : $"{flightScene.CraftBiomeData.BiomeName}, {flightScene.CraftBiomeData.SubBiomeName}";
        //craftNode.Velocity
        trajectoryType = GetTrajectoryType();
    }

    private string GetTrajectoryType()
    {
        ICraftNode craftNode = flightScene.CraftNode;
        string planetName = craftNode.Parent.Name;
        // Grounded

        if (craftNode.CraftScript.ActiveCommandPod.IsEva)
        {
            if (craftNode.InContactWithPlanet)
            {
                return $"EVA on {planetName} Surface";
            }
            return $"EVA Above {planetName}";
        }
        else if (craftNode.InContactWithWater)
        {
            if (craftNode.AltitudeAgl < -10)
            {
                return $"Submerged Below {planetName}";
            }
            return $"Sailing on {planetName}";
        }
        else if (craftNode.InContactWithPlanet)
        {
            return $"On the Surface of {planetName}";
        }
        // Water Grounded
        else
        {
            IPlanetData planetData = craftNode.Parent.PlanetData;
            IOrbit craftOrbit = craftNode.Orbit;
            double planetRadius = planetData.Radius;
            double craftAp = craftOrbit.ApoapsisDistance - planetRadius;
            double craftPe = craftOrbit.PeriapsisDistance - planetRadius;
            double dayLength = -2 * Mathf.PI / planetNode.PlanetData.AngularVelocity;
            double period = craftOrbit.Period;
            // Suborbital
            if (double.IsNaN(craftAp) || double.IsInfinity(craftAp))
            {
                return $"On {planetName} Escape";
            }
            else if (craftPe < craftNode.Parent.PlanetData.AtmosphereData.Height || craftPe < 0)
            {
                return GetFlightType(craftNode.Parent.PlanetData.AtmosphereData.Height, craftAp, craftNode.Altitude, planetName, planetRadius);
            }
            else
            {
                if (GetWithinPercentage(period, dayLength, 0.01))
                {
                    return $"In {planetName} Synchronous Orbit";
                }
                string ApState = GetOrbitType(craftAp, planetRadius);
                string PeState = GetOrbitType(craftPe, planetRadius);
                if (PeState == ApState)
                {
                    return $"In {ApState} {planetName} Orbit";
                }
                return $"In {PeState}-{ApState} {planetName} Orbit";
            }
        }
    }

    public bool GetWithinPercentage(double a, double b, double percentage)
    {
        double maxVal = b * (1 + percentage);
        double minVal = b * (1 - percentage);
        return a >= minVal && a <= maxVal;
    }

    public string GetFlightType(double atmosphereHeight, double craftAp, double altitude, string bodyName, double planetRadius)
    {
        string flightType = "Flying";
        bool isAtmosphereic = atmosphereHeight > 0;
        double[] nums1 = isAtmosphereic ? atmosphereicFlight : vacuumFlight;
        double num2 = isAtmosphereic ? atmosphereHeight : planetRadius;
        double num3 = isAtmosphereic ? atmosphereHeight : planetRadius * 7.5e-5;
        if (craftAp > num3 || altitude > nums1[2] * num2)
        {
            flightType = "In Sub-Orbital Flight";
        }
        else if (altitude < nums1[0] * num2)
        {
            flightType = "Flying Low";
        }
        else if (altitude < nums1[1] * num2)
        {
            flightType = "Flying";
        }
        else if (altitude < nums1[2] * num2)
        {
            flightType = "Flying High";
        }
        else
        {
            flightType = "Flying";
        }
        return $"{flightType} Over {bodyName}";
    }

    public string GetOrbitType(double orbitValue, double planetRadius)
    {
        string orbitType = "";
        if (orbitValue <= planetRadius * 0.33)
        {
            orbitType = "Low";
        }
        else if (orbitValue <= planetRadius * 5)
        {
            orbitType = "Medium";
        }
        else if (orbitValue >= planetRadius * 5)
        {
            orbitType = "High";
        }
        return orbitType;
    }
}