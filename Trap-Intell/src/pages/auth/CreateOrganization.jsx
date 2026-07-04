import { useState } from "react";
import { useNavigate } from "react-router-dom";
import styles from "./CreateOrganization.module.css";
import lockImage from "../../assets/images/lock.png";
import { api } from "../../services/api";
import Header from "../../components/Header";
import Logo from "../../components/Logo";

const organizationTypes = [
  { value: 0, label: "SMB" },
  { value: 1, label: "Educational" },
  { value: 2, label: "NGO" },
  { value: 3, label: "Government" },
  { value: 4, label: "Enterprise" },
  { value: 5, label: "Startup" },
  { value: 6, label: "Other" },
];

const companySizes = [
  { value: 10, label: "1-10 Employees" },
  { value: 50, label: "11-50 Employees" },
  { value: "200", label: "51-200 Employees" },
  { value: 500, label: "201-500 Employees" },
  { value: 1000, label: "500+ Employees" },
];

const CreateOrganization = () => {
  const [loading, setLoading] = useState(false);
  const [acceptedTerms, setAcceptedTerms] = useState(false);
  const [shake, setShake] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const [formData, setFormData] = useState({
    name: "",
    type: 4,
    industry: "",
    size: "",
    domain: "",
    taxId: "",
    contactEmail: "",
    contactPhone: "",
    contactWebsite: "",
    website: "",
    allowMultipleAddresses: false,
    requireApprovalForMembers: false,
    maximumMembers: 100,
    enableBilling: false,
    enableApiAccess: false,
    parentOrganizationId: null,
  });

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;

    setFormData((prev) => ({
      ...prev,
      [name]:
        type === "checkbox"
          ? checked
          : ["type", "size", "maximumMembers"].includes(name)
            ? Number(value)
            : value,
    }));
  };

  const handleCreate = async () => {
    if (!acceptedTerms) {
      setShake(true);
      setTimeout(() => setShake(false), 400);
      return;
    }

    setError("");
    setSuccess("");

    if (
      !formData.name ||
      !formData.domain ||
      !formData.taxId ||
      !formData.contactEmail ||
      !formData.contactPhone
    ) {
      setError("Please fill in all required fields.");
      return;
    }

    try {
      setLoading(true);

      const response = await api.post("/api/organizations", {
        name: formData.name,
        type: Number(formData.type),
        industry: formData.industry,
        size: Number(formData.size),
        domain: formData.domain,
        taxId: formData.taxId,
        contactEmail: formData.contactEmail,
        contactPhone: formData.contactPhone,
        contactWebsite: formData.contactWebsite || null,
        website: formData.website || null,
        allowMultipleAddresses: formData.allowMultipleAddresses,
        requireApprovalForMembers: formData.requireApprovalForMembers,
        maximumMembers: Number(formData.maximumMembers),
        enableBilling: formData.enableBilling,
        enableApiAccess: formData.enableApiAccess,
        parentOrganizationId: formData.parentOrganizationId,
      });

      const organizationId = response?.id || response;
      console.log("Organization created:", organizationId);

      navigate("/check-email", {
        state: {
          organizationId,
          email: formData.contactEmail,
        },
      });

      setFormData({
        name: "",
        type: 4,
        industry: "",
        size: "",
        domain: "",
        taxId: "",
        contactEmail: "",
        contactPhone: "",
        contactWebsite: "",
        website: "",
        allowMultipleAddresses: false,
        requireApprovalForMembers: false,
        maximumMembers: 100,
        enableBilling: false,
        enableApiAccess: false,
        parentOrganizationId: null,
      });

      setAcceptedTerms(false);
    } catch (err) {
      console.error(err);
      setError(err.message || "Failed to create organization.");
    } finally {
      setLoading(false);
    }
  };

  const navigate = useNavigate();

  return (
    <div className={styles.container}>
      <div className={styles.binaryBg}></div>

      <header className={styles.header}>
        <div className={styles.logo}>
          <Logo />
        </div>
      </header>

      <main className={styles.page}>
        {/* Left Panel */}
        <div className={styles.leftPanel}>
          <div className={styles.formCard}>
            <div className={styles.formHeader}>
              <h1 className={styles.formTitle}>Create Organization</h1>

              <p className={styles.formSubtitle}>
                Get started with enterprise-grade security monitoring.
              </p>
            </div>

            {error && <div className={styles.errorMessage}>{error}</div>}

            {success && <div className={styles.successMessage}>{success}</div>}

            <div className={styles.formGrid}>
              <div className={`${styles.formGroup} ${styles.full}`}>
                <div className={styles.fieldWrap}>
                  <input
                    name="name"
                    value={formData.name}
                    onChange={handleChange}
                    placeholder="Organization Name"
                  />
                  <label>Organization Name *</label>
                </div>
              </div>

              <div className={styles.formGroup}>
                <div className={styles.fieldWrap}>
                  <select
                    name="type"
                    value={formData.type}
                    onChange={handleChange}
                  >
                    {organizationTypes.map((type) => (
                      <option key={type.value} value={type.value}>
                        {type.label}
                      </option>
                    ))}
                  </select>
                  <label>Organization Type</label>
                </div>
              </div>

              <div className={styles.formGroup}>
                <div className={styles.fieldWrap}>
                  <select
                    name="size"
                    value={formData.size}
                    onChange={handleChange}
                  >
                    <option value="">Select Company Size</option>

                    {companySizes.map((size) => (
                      <option key={size.value} value={size.value}>
                        {size.label}
                      </option>
                    ))}
                  </select>
                  <label>Company Size</label>
                </div>
              </div>

              <div className={`${styles.formGroup} ${styles.full}`}>
                <div className={styles.fieldWrap}>
                  <input
                    name="industry"
                    value={formData.industry}
                    onChange={handleChange}
                    placeholder="Industry"
                  />
                  <label>Industry</label>
                </div>
              </div>

              <div className={styles.formGroup}>
                <div className={styles.fieldWrap}>
                  <input
                    name="domain"
                    value={formData.domain}
                    onChange={handleChange}
                    placeholder="company.com"
                  />
                  <label>Domain *</label>
                </div>
              </div>

              <div className={styles.formGroup}>
                <div className={styles.fieldWrap}>
                  <input
                    name="taxId"
                    value={formData.taxId}
                    onChange={handleChange}
                    placeholder="Tax ID"
                  />
                  <label>Tax ID *</label>
                </div>
              </div>

              <div className={styles.formGroup}>
                <div className={styles.fieldWrap}>
                  <input
                    type="email"
                    name="contactEmail"
                    value={formData.contactEmail}
                    onChange={handleChange}
                    placeholder="admin@company.com"
                  />
                  <label>Contact Email *</label>
                </div>
              </div>

              <div className={styles.formGroup}>
                <div className={styles.fieldWrap}>
                  <input
                    name="contactPhone"
                    value={formData.contactPhone}
                    onChange={handleChange}
                    placeholder="+20 100 000 0000"
                  />
                  <label>Contact Phone *</label>
                </div>
              </div>

              <div className={`${styles.formGroup} ${styles.full}`}>
                <div className={styles.fieldWrap}>
                  <input
                    name="contactWebsite"
                    value={formData.contactWebsite}
                    onChange={handleChange}
                    placeholder="https://contact.company.com"
                  />
                  <label>Contact Website</label>
                </div>
              </div>

              <div className={`${styles.formGroup} ${styles.full}`}>
                <div className={styles.fieldWrap}>
                  <input
                    name="website"
                    value={formData.website}
                    onChange={handleChange}
                    placeholder="https://company.com"
                  />
                  <label>Website</label>
                </div>
              </div>

              <div className={`${styles.formGroup} ${styles.full}`}>
                <div className={styles.fieldWrap}>
                  <input
                    type="number"
                    min={1}
                    name="maximumMembers"
                    value={formData.maximumMembers}
                    onChange={handleChange}
                  />
                  <label>Maximum Members</label>
                </div>
              </div>

              <div className={`${styles.formGroup} ${styles.full}`}>
                <label className={styles.checkboxRow}>
                  <input
                    type="checkbox"
                    name="allowMultipleAddresses"
                    checked={formData.allowMultipleAddresses}
                    onChange={handleChange}
                  />
                  <span className={styles.checkboxLabel}>
                    Allow Multiple Addresses
                  </span>
                </label>

                <label className={styles.checkboxRow}>
                  <input
                    type="checkbox"
                    name="requireApprovalForMembers"
                    checked={formData.requireApprovalForMembers}
                    onChange={handleChange}
                  />
                  <span className={styles.checkboxLabel}>
                    Require Approval For Members
                  </span>
                </label>

                <label className={styles.checkboxRow}>
                  <input
                    type="checkbox"
                    name="enableBilling"
                    checked={formData.enableBilling}
                    onChange={handleChange}
                  />
                  <span className={styles.checkboxLabel}>Enable Billing</span>
                </label>

                <label className={styles.checkboxRow}>
                  <input
                    type="checkbox"
                    name="enableApiAccess"
                    checked={formData.enableApiAccess}
                    onChange={handleChange}
                  />
                  <span className={styles.checkboxLabel}>
                    Enable API Access
                  </span>
                </label>
              </div>

              <div className={`${styles.formGroup} ${styles.full}`}>
                <label
                  className={`${styles.checkboxRow} ${
                    shake ? styles.shake : ""
                  }`}
                >
                  <input
                    type="checkbox"
                    checked={acceptedTerms}
                    onChange={(e) => setAcceptedTerms(e.target.checked)}
                  />

                  <span className={styles.checkboxLabel}>
                    I agree to all Terms and Privacy Policies
                  </span>
                </label>
              </div>

              <div className={`${styles.formGroup} ${styles.full}`}>
                <div className={styles.btnRow}>
                  <button
                    type="button"
                    className={styles.btnBack}
                    onClick={() => window.history.back()}
                  >
                    Back
                  </button>

                  <button
                    type="button"
                    className={styles.btnCreate}
                    disabled={loading}
                    onClick={handleCreate}
                  >
                    {loading ? "Creating..." : "Create Organization"}
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Right Panel */}
        <div className={styles.rightPanel}>
          <div className={styles.heroImageWrap}>
            <img src={lockImage} alt="Cybersecurity Shield" />
          </div>
        </div>
      </main>
    </div>
  );
};

export default CreateOrganization;
