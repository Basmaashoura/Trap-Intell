import { useState } from "react";
import styles from "./ThreatActorsPage.module.css";

const STATIC_ACTORS = [
  {
    id: 1,
    name: "APT29",
    alias: "Cozy Bear",
    score: 92,
    country: "Russia",
    status: "Active",
    sectors: ["Government", "Finance", "Energy"],
    lastSeen: "2 hours ago",
  },
  {
    id: 2,
    name: "Lazarus Group",
    alias: "Hidden Cobra",
    score: 88,
    country: "North Korea",
    status: "Active",
    sectors: ["Cryptocurrency", "Banking"],
    lastSeen: "1 day ago",
  },
];

export default function ThreatActorsPage() {
  const [actors] = useState(STATIC_ACTORS);
  const [search, setSearch] = useState("");
  const [showAddModal, setShowAddModal] = useState(false);
  const [selectedActor, setSelectedActor] = useState(null);

  const filtered = actors.filter(
    (a) =>
      a.name.toLowerCase().includes(search.toLowerCase()) ||
      a.alias.toLowerCase().includes(search.toLowerCase()),
  );

  return (
    <div>
      {/* Header */}
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "flex-start",
          marginBottom: 24,
        }}
      >
        <div>
          <h1 style={{ margin: 0 }}>Threat Actors</h1>
          <p style={{ marginTop: 6, color: "#6b7280" }}>
            Monitor and manage known threat actor profiles.
          </p>
        </div>

        <button
          className={styles.btnAddProfile}
          onClick={() => setShowAddModal(true)}
        >
          + Add Profile
        </button>
      </div>

      {/* Search */}
      <div className={styles.actorSearchWrap}>
        <input
          className={styles.actorSearchInput}
          placeholder="Search actors..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      {/* Grid */}
      <div className={styles.actorsGrid} style={{ marginTop: 20 }}>
        {filtered.map((actor) => (
          <div key={actor.id} className={styles.actorCard}>
            <div className={styles.actorCardTop}>
              <div
                className={styles.actorAvatar}
                style={{
                  background: "linear-gradient(135deg,#4044e4,#8a1bfa)",
                }}
              >
                {actor.name.slice(0, 2)}
              </div>

              <div className={styles.actorIdentity}>
                <div className={styles.actorName}>{actor.name}</div>
                <div className={styles.actorAlias}>{actor.alias}</div>
              </div>

              <div className={styles.actorScoreCol}>
                <div className={styles.actorScoreNum}>{actor.score}</div>
                <div className={styles.actorScoreLbl}>Risk Score</div>
              </div>
            </div>

            <div className={styles.actorMeta}>
              <div className={styles.actorMetaRow}>
                <span className={styles.actorMetaKey}>Country</span>
                <span className={styles.actorMetaVal}>{actor.country}</span>
              </div>

              <div className={styles.actorMetaRow}>
                <span className={styles.actorMetaKey}>Status</span>
                <span
                  className={`${styles.actorMetaVal} ${
                    actor.status === "Active"
                      ? styles.statusActive
                      : styles.statusDormant
                  }`}
                >
                  {actor.status}
                </span>
              </div>
            </div>

            <div>
              <div className={styles.sectorsLabel}>Targeted Sectors</div>

              <div className={styles.sectorsChips}>
                {actor.sectors.map((sector) => (
                  <span key={sector} className={styles.sectorChip}>
                    {sector}
                  </span>
                ))}
              </div>
            </div>

            <div className={styles.actorCardFooter}>
              <span className={styles.actorLastSeen}>
                Last seen {actor.lastSeen}
              </span>

              <button
                className={styles.btnViewIntel}
                onClick={() => setSelectedActor(actor)}
              >
                View Intelligence →
              </button>
            </div>
          </div>
        ))}
      </div>

      {/* Add Modal */}
      {showAddModal && (
        <div
          className={styles.modalOverlay}
          onClick={() => setShowAddModal(false)}
        >
          <div
            className={styles.addActorCard}
            onClick={(e) => e.stopPropagation()}
          >
            <h3 className={styles.addActorTitle}>Add New Threat Actor</h3>

            <div className={styles.addActorFields}>
              <div className={styles.addActorField}>
                <label>Name</label>
                <input placeholder="Actor name" />
              </div>

              <div className={styles.addActorField}>
                <label>Alias</label>
                <input placeholder="Alias" />
              </div>

              <div className={styles.addActorField}>
                <label>Country</label>
                <input placeholder="Country" />
              </div>
            </div>

            <div className={styles.addActorBtns}>
              <button
                className={styles.btnCancelModal}
                onClick={() => setShowAddModal(false)}
              >
                Cancel
              </button>

              <button className={styles.btnCreateModal}>Create</button>
            </div>
          </div>
        </div>
      )}

      {/* Intelligence Modal */}
      {selectedActor && (
        <div
          className={styles.modalOverlay}
          onClick={() => setSelectedActor(null)}
        >
          <div
            className={styles.intelCard}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={styles.intelHeader}>
              <div
                className={styles.intelAvatar}
                style={{
                  background: "linear-gradient(135deg,#4044e4,#8a1bfa)",
                }}
              >
                {selectedActor.name.slice(0, 2)}
              </div>

              <div className={styles.intelHeaderInfo}>
                <div className={styles.intelName}>{selectedActor.name}</div>

                <div className={styles.intelBadges}>
                  <span className={styles.badgeAlias}>
                    {selectedActor.alias}
                  </span>
                </div>
              </div>

              <button
                className={styles.intelClose}
                onClick={() => setSelectedActor(null)}
              >
                ✕
              </button>
            </div>

            <div className={styles.intelBody}>
              <div className={styles.intelStats}>
                <div className={styles.intelStat}>
                  <div className={styles.intelStatLbl}>Risk Score</div>
                  <div className={styles.intelStatVal}>
                    {selectedActor.score}
                  </div>
                </div>

                <div className={styles.intelStat}>
                  <div className={styles.intelStatLbl}>Status</div>
                  <div className={styles.intelStatVal}>
                    {selectedActor.status}
                  </div>
                </div>

                <div className={styles.intelStat}>
                  <div className={styles.intelStatLbl}>Country</div>
                  <div className={styles.intelStatVal}>
                    {selectedActor.country}
                  </div>
                </div>
              </div>

              <div>
                <div className={styles.intelSecTitle}>Targeted Sectors</div>

                <div className={styles.intelChips}>
                  {selectedActor.sectors.map((sector) => (
                    <span
                      key={sector}
                      className={`${styles.chip} ${styles.chipSector}`}
                    >
                      {sector}
                    </span>
                  ))}
                </div>
              </div>
            </div>

            <div className={styles.intelFooter}>
              <button
                className={styles.btnCloseIntel}
                onClick={() => setSelectedActor(null)}
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
