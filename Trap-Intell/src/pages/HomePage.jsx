import NavBar from "../components/Header";
import Hero from "../components/Hero";
import ProjectOverview from "../components/ProjectOverview";
import AboutUs from "../components/AboutUs";
import Services from "../components/Services";
import CoreFeatures from "../components/CoreFeatures";
import Pricing from "../components/Pricing";
import Footer from "../components/Footer";

function HomePage() {
  return (
    <>
      <NavBar />
      <Hero />
      <ProjectOverview />
      <AboutUs />
      <Services />
      <CoreFeatures />
      <Pricing />
      {/* cta */}
      <Footer />
    </>
  );
}

export default HomePage;
