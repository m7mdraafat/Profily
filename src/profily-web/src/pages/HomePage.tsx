import { useNavigate } from 'react-router-dom';
import { 
  FileCode2, 
  LayoutDashboard, 
  Palette, 
  BarChart3, 
  Rocket, 
  RefreshCw, 
  Smartphone, 
  Search,
  ArrowRight,
  ChevronRight,
  Github
} from 'lucide-react';
import { Badge } from '../components';
import { styles } from './HomePage.styles';

export function HomePage() {
  const navigate = useNavigate();

  return (
    <div className={styles.wrapper}>
      {/* Hero Section */}
      <section className={styles.hero.section}>
        <h1 className={styles.hero.title}>
          Create stunning <span className={styles.hero.gradient}>GitHub profiles</span> and <span className={styles.hero.gradient}>portfolios</span>
        </h1>
        <p className={styles.hero.subtitle}>
          Transform your GitHub presence in minutes. No coding required.
        </p>
      </section>

      {/* Gradient Divider */}
      <div className={styles.divider} />

      {/* Workflow Section */}
      <section>
        <h2 className={styles.workflow.title}>How It Works</h2>
        <p className={styles.workflow.subtitle}>Three simple steps</p>

        <div className={styles.workflow.container}>
          {/* Step 1 - Connect */}
          <div className={styles.workflow.stepWrapper}>
            <div className={styles.card.base}>
              <div className={`${styles.iconBox.base} ${styles.iconBox.github}`}>
                <Github size={22} />
              </div>
              <div>
                <h3 className={styles.stepText.title}>Connect GitHub</h3>
                <p className={styles.stepText.description}>Sign in to import your repos and stats.</p>
              </div>
            </div>
          </div>

          {/* Arrow 1 */}
          <div className={styles.workflow.arrow}>
            <ChevronRight size={20} className={styles.workflow.arrowIcon} />
          </div>

          {/* Step 2 - Choose */}
          <div className={styles.workflow.stepWrapper}>
            <div className={styles.card.choose}>
              <span className={styles.card.chooseLabel}>Choose what to build</span>
              <div className={styles.card.chooseButtons}>
                <button
                  onClick={() => navigate('/profile')}
                  className={`${styles.stepButton.base} ${styles.stepButton.profile}`}
                >
                  <div className={styles.iconBox.profile}>
                    <FileCode2 size={18} />
                  </div>
                  <span className={styles.stepButton.text}>Profile README</span>
                  <ArrowRight size={14} className={styles.stepButton.arrow} />
                </button>
                <span className={styles.card.or}>or</span>
                <button
                  onClick={() => navigate('/portfolio')}
                  className={`${styles.stepButton.base} ${styles.stepButton.portfolio}`}
                >
                  <div className={styles.iconBox.portfolio}>
                    <LayoutDashboard size={18} />
                  </div>
                  <span className={styles.stepButton.text}>Portfolio Website</span>
                  <ArrowRight size={14} className={styles.stepButton.arrow} />
                </button>
              </div>
            </div>
          </div>

          {/* Arrow 2 */}
          <div className={styles.workflow.arrow}>
            <ChevronRight size={20} className={styles.workflow.arrowIcon} />
          </div>

          {/* Step 3 - Deploy */}
          <div className={styles.workflow.stepWrapper}>
            <div className={styles.card.base}>
              <div className={`${styles.iconBox.base} ${styles.iconBox.rocket}`}>
                <Rocket size={22} />
              </div>
              <div>
                <h3 className={styles.stepText.title}>Deploy & Share</h3>
                <p className={styles.stepText.description}>One-click deploy to GitHub Pages.</p>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Gradient Divider */}
      <div className={styles.divider} />

      {/* Features */}
      <section className={styles.features.section}>
        <h3 className={styles.features.title}>Platform Features</h3>
        <div className={styles.features.badges}>
          <Badge icon={<Palette size={14} className="text-pink-400" />}>Multiple Themes</Badge>
          <Badge icon={<BarChart3 size={14} className="text-emerald-400" />}>Dynamic Stats</Badge>
          <Badge icon={<Rocket size={14} className="text-orange-400" />}>GitHub Pages Deploy</Badge>
          <Badge icon={<RefreshCw size={14} className="text-cyan-400" />}>Auto-Updates</Badge>
          <Badge icon={<Smartphone size={14} className="text-purple-400" />}>Mobile Responsive</Badge>
          <Badge icon={<Search size={14} className="text-yellow-400" />}>SEO Optimized</Badge>
        </div>
      </section>
    </div>
  );
}