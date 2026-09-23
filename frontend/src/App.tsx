import { useState, useEffect } from 'react';
import { JarvisHeroScreen } from './components/JarvisHeroScreen';
import { BuilderIntakeScreen } from './components/BuilderIntakeScreen';
import { Stage2Workspace } from './components/Stage2Workspace';
import { AppCategory } from './types';
import { soundFX } from './utils/audio';

type Stage = 'hero' | 'intake' | 'builder';

export default function App() {
  const [stage, setStage] = useState<Stage>('hero');
  const [activeDirective, setActiveDirective] = useState('');
  const [appType, setAppType] = useState<AppCategory | null>(null);

  const startFromHero = (directive: string) => {
    const trimmed = directive.trim();
    if (!trimmed) return;
    setActiveDirective(trimmed);
    setAppType(null);
    setStage('builder');
  };

  const startFromIntake = (directive: string, type: AppCategory) => {
    const trimmed = directive.trim();
    if (!trimmed) return;
    setActiveDirective(trimmed);
    setAppType(type);
    setStage('builder');
  };

  const returnHome = () => {
    soundFX.playPowerDown();
    setStage('hero');
  };

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && e.altKey && stage !== 'hero') returnHome();
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [stage]);

  return (
    <div className="w-screen h-screen overflow-hidden bg-[#030508] text-[#F5F8FF] selection:bg-[#2F7CFF]/30 font-sans">
      {stage === 'hero' && (
        <JarvisHeroScreen onStartBuild={startFromHero} onOpenBuilder={() => setStage('intake')} />
      )}
      {stage === 'intake' && <BuilderIntakeScreen onBuild={startFromIntake} />}
      {stage === 'builder' && (
        <Stage2Workspace
          initialPrompt={activeDirective}
          appType={appType}
          onReturnHome={returnHome}
          onOpenIntake={() => setStage('intake')}
        />
      )}
    </div>
  );
}
